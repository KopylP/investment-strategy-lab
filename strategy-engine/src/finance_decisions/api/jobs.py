"""Background runs of /decide and /risk, for strategies that think for minutes or hours (an AI agent).

POST /v1/strategies/{code}/decide/jobs (or /risk/jobs) checks the strategy and its parameters at once and answers
202 with a job id; GET /v1/jobs/{jobId} says whether the job still runs and, when it is done, gives its answer.
Jobs live in memory of the one worker process (uvicorn --workers 1): after a restart the engine no longer knows them
(404), and the caller submits the job again. Finished jobs are forgotten after ``JOB_RETENTION``.
"""

from __future__ import annotations

import os
import threading
import uuid
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from typing import Any, Callable

from fastapi import APIRouter, HTTPException, status

from finance_decisions.strategies.errors import ContextError
from .catalog import find_strategy_type
from .contracts import EvaluationContext, JobError, JobStatus, StrategyJob
from .price_sets import with_prices

JOB_RETENTION = timedelta(days=7)

router = APIRouter(prefix="/v1", tags=["jobs"])


@dataclass
class _Job:
    job_id: str
    code: str
    action: str
    status: JobStatus = JobStatus.RUNNING
    result: dict[str, Any] | None = None
    error: JobError | None = None
    finished_at: datetime | None = None

    def view(self) -> StrategyJob:
        return StrategyJob(job_id=self.job_id, code=self.code, action=self.action, status=self.status, result=self.result, error=self.error)


@dataclass
class JobStore:
    workers: int = field(default_factory=lambda: int(os.environ.get("STRATEGY_JOB_WORKERS", "4")))
    _jobs: dict[str, _Job] = field(default_factory=dict)
    _lock: threading.Lock = field(default_factory=threading.Lock)
    _pool: ThreadPoolExecutor | None = None

    def submit(self, code: str, action: str, run: Callable[[], Any]) -> StrategyJob:
        job = _Job(job_id=uuid.uuid4().hex, code=code, action=action)
        with self._lock:
            self._forget_old()
            self._jobs[job.job_id] = job
            if self._pool is None:
                self._pool = ThreadPoolExecutor(max_workers=self.workers, thread_name_prefix="strategy-job")
        self._pool.submit(self._run, job, run)
        return job.view()

    def find(self, job_id: str) -> StrategyJob | None:
        with self._lock:
            job = self._jobs.get(job_id)
            return job.view() if job else None

    def _run(self, job: _Job, run: Callable[[], Any]) -> None:
        try:
            result = run().model_dump(by_alias=True, mode="json")
            outcome = (JobStatus.SUCCEEDED, result, None)
        except ContextError as error:
            outcome = (JobStatus.FAILED, None, JobError(status_code=status.HTTP_422_UNPROCESSABLE_CONTENT, detail=str(error)))
        except Exception as error:  # noqa: BLE001 — whatever breaks inside a strategy is the job's answer
            outcome = (JobStatus.FAILED, None, JobError(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=f"{type(error).__name__}: {error}"))
        with self._lock:
            job.status, job.result, job.error = outcome
            job.finished_at = datetime.now(timezone.utc)

    def _forget_old(self) -> None:
        cutoff = datetime.now(timezone.utc) - JOB_RETENTION
        for job_id in [job.job_id for job in self._jobs.values() if job.finished_at and job.finished_at < cutoff]:
            del self._jobs[job_id]


store = JobStore()


def get_job_store() -> JobStore:
    return store


def _submit(code: str, action: str, context: EvaluationContext) -> StrategyJob:
    strategy_type = find_strategy_type(code)
    parameters = strategy_type.parse_parameters(context.parameters)
    run = strategy_type.decide if action == "decide" else strategy_type.assess_risk
    priced = with_prices(context)
    return get_job_store().submit(code, action, lambda: run(priced, parameters))


@router.post("/strategies/{code}/decide/jobs", response_model=StrategyJob, status_code=status.HTTP_202_ACCEPTED)
def submit_decision(code: str, context: EvaluationContext) -> StrategyJob:
    return _submit(code, "decide", context)


@router.post("/strategies/{code}/risk/jobs", response_model=StrategyJob, status_code=status.HTTP_202_ACCEPTED)
def submit_risk_assessment(code: str, context: EvaluationContext) -> StrategyJob:
    return _submit(code, "risk", context)


@router.get("/jobs/{job_id}", response_model=StrategyJob)
def get_job(job_id: str) -> StrategyJob:
    if (job := get_job_store().find(job_id)) is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, detail=f"Unknown job '{job_id}' (finished long ago, or the engine restarted).")
    return job
