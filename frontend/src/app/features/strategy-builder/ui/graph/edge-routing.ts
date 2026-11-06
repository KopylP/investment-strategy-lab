export interface Point {
  x: number;
  y: number;
}
export interface NodeBox extends Point {
  id: string;
  width: number;
  height: number;
}

// Keep the route's existing channels while its sockets move. Searching for a
// fresh shortest path on every pointer event makes equally good routes flicker.
export function retargetRoute(points: Point[], start: Point, end: Point): Point[] {
  const first = points[0],
    last = points[points.length - 1];
  const dx = start.x - first.x,
    dy = start.y - first.y;
  if (Math.abs(dx - (end.x - last.x)) < 0.01 && Math.abs(dy - (end.y - last.y)) < 0.01)
    return points.map((p) => ({ x: p.x + dx, y: p.y + dy }));
  const bends: Point[] = [];
  for (const point of points) {
    const previous = bends[bends.length - 1];
    if (previous?.x === point.x && previous.y === point.y) continue;
    while (bends.length > 1) {
      const a = bends[bends.length - 2],
        b = bends[bends.length - 1];
      if (!((a.x === b.x && b.x === point.x) || (a.y === b.y && b.y === point.y))) break;
      bends.pop();
    }
    bends.push({ ...point });
  }
  if (bends.length <= 2) {
    const x = (first.x + last.x) / 2;
    return [start, { x, y: start.y }, { x, y: end.y }, end];
  }
  bends[0] = { ...start };
  bends[bends.length - 1] = { ...end };
  bends[1].y = start.y;
  bends[bends.length - 2].y = end.y;
  if (bends.length === 4 && start.x + 48 <= end.x) {
    const lane = Math.max(start.x + 24, Math.min(end.x - 24, bends[1].x));
    bends[1].x = bends[2].x = lane;
  } else if (bends.length > 4) {
    bends[1].x = Math.max(start.x + 24, bends[1].x);
    bends[2].x = bends[1].x;
    bends[bends.length - 2].x = Math.min(end.x - 24, bends[bends.length - 2].x);
    bends[bends.length - 3].x = bends[bends.length - 2].x;
  }
  return bends;
}

export function routeIsClear(
  points: Point[],
  boxes: NodeBox[],
  source: string,
  target: string,
): boolean {
  return !boxes.some((box) =>
    points.slice(1).some((b, index) => {
      // The socket's own short horizontal exit is allowed through its card margin.
      if ((box.id === source && index === 0) || (box.id === target && index === points.length - 2))
        return false;
      const a = points[index],
        padding = 8;
      const left = box.x - padding,
        right = box.x + box.width + padding;
      const top = box.y - padding,
        bottom = box.y + box.height + padding;
      if (a.x === b.x)
        return a.x > left && a.x < right && Math.max(a.y, b.y) > top && Math.min(a.y, b.y) < bottom;
      return a.y > top && a.y < bottom && Math.max(a.x, b.x) > left && Math.min(a.x, b.x) < right;
    }),
  );
}

export function roundedPath(points: Point[]): string {
  const clean: Point[] = [];
  for (const point of points) {
    const last = clean[clean.length - 1];
    if (last && last.x === point.x && last.y === point.y) continue;
    while (clean.length >= 2) {
      const a = clean[clean.length - 2],
        b = clean[clean.length - 1];
      if (!((a.x === b.x && b.x === point.x) || (a.y === b.y && b.y === point.y))) break;
      clean.pop();
    }
    clean.push(point);
  }
  if (clean.length < 2) return '';
  let path = `M ${clean[0].x} ${clean[0].y}`;
  for (let i = 1; i < clean.length - 1; i++) {
    const a = clean[i - 1],
      b = clean[i],
      c = clean[i + 1];
    const d = clean[i + 2];
    if (
      d &&
      a.y === b.y &&
      b.x === c.x &&
      c.y === d.y &&
      Math.abs(c.y - b.y) <= 8 &&
      (b.x - a.x) * (d.x - c.x) > 0
    ) {
      // A few pixels of port/layout mismatch should read as one smooth line,
      // instead of a conspicuous miniature staircase in a long horizontal run.
      const e = clean[i + 3];
      const corner = e && e.x === d.x;
      const radius = corner ? Math.min(10, Math.abs(d.x - c.x) / 2, Math.abs(e.y - d.y) / 2) : 0;
      const endX = d.x - Math.sign(d.x - c.x) * radius;
      path += ` C ${b.x} ${a.y} ${c.x} ${d.y} ${endX} ${d.y}`;
      if (corner) path += ` Q ${d.x} ${d.y} ${d.x} ${d.y + Math.sign(e.y - d.y) * radius}`;
      i += 2;
      continue;
    }
    const before = Math.hypot(b.x - a.x, b.y - a.y),
      after = Math.hypot(c.x - b.x, c.y - b.y);
    const radius = Math.min(10, before / 2, after / 2);
    const p = {
      x: b.x + ((a.x - b.x) * radius) / before,
      y: b.y + ((a.y - b.y) * radius) / before,
    };
    const q = { x: b.x + ((c.x - b.x) * radius) / after, y: b.y + ((c.y - b.y) * radius) / after };
    path += ` L ${p.x} ${p.y} Q ${b.x} ${b.y} ${q.x} ${q.y}`;
  }
  const last = clean[clean.length - 1];
  return `${path} L ${last.x} ${last.y}`;
}

// Used for saved layouts and after dragging. Route through free channels between cards;
// the first and last horizontal segments keep the path attached to the real DOM sockets.
export function routePoints(
  start: Point,
  end: Point,
  boxes: NodeBox[],
  source: string,
  target: string,
): Point[] {
  const from = boxes.find((box) => box.id === source),
    to = boxes.find((box) => box.id === target);
  if (!from || !to)
    return [
      start,
      { x: (start.x + end.x) / 2, y: start.y },
      { x: (start.x + end.x) / 2, y: end.y },
      end,
    ];
  const a = { x: Math.max(start.x + 24, from.x + from.width + 24), y: start.y };
  const b = { x: Math.min(end.x - 24, to.x - 24), y: end.y };
  const obstacles = boxes.map((box) => ({
    left: box.x - 12,
    right: box.x + box.width + 12,
    top: box.y - 12,
    bottom: box.y + box.height + 12,
  }));
  const xs = [...new Set([a.x, b.x, ...obstacles.flatMap((r) => [r.left, r.right])])].sort(
    (x, y) => x - y,
  );
  const ys = [...new Set([a.y, b.y, ...obstacles.flatMap((r) => [r.top, r.bottom])])].sort(
    (x, y) => x - y,
  );
  const point = (id: number): Point => ({
    x: xs[id % xs.length],
    y: ys[Math.floor(id / xs.length)],
  });
  const clear = (p: Point, q: Point) =>
    !obstacles.some((r) =>
      p.x === q.x
        ? p.x > r.left &&
          p.x < r.right &&
          Math.max(p.y, q.y) > r.top &&
          Math.min(p.y, q.y) < r.bottom
        : p.y > r.top &&
          p.y < r.bottom &&
          Math.max(p.x, q.x) > r.left &&
          Math.min(p.x, q.x) < r.right,
    );
  const first = ys.indexOf(a.y) * xs.length + xs.indexOf(a.x),
    last = ys.indexOf(b.y) * xs.length + xs.indexOf(b.x);
  // State includes arrival direction so a bend has a cost and paths stay simple.
  const queue = [{ key: first * 2, cost: 0, score: Math.abs(a.x - b.x) + Math.abs(a.y - b.y) }];
  const costs = new Map([[first * 2, 0]]),
    parents = new Map<number, number>();
  let found: number | undefined;
  while (queue.length) {
    queue.sort((p, q) => q.score - p.score);
    const current = queue.pop()!;
    if (current.cost !== costs.get(current.key)) continue;
    const id = Math.floor(current.key / 2),
      direction = current.key % 2,
      p = point(id);
    if (id === last) {
      found = current.key;
      break;
    }
    const col = id % xs.length,
      row = Math.floor(id / xs.length);
    for (const [next, nextDirection] of [
      [col > 0 ? id - 1 : -1, 0],
      [col < xs.length - 1 ? id + 1 : -1, 0],
      [row > 0 ? id - xs.length : -1, 1],
      [row < ys.length - 1 ? id + xs.length : -1, 1],
    ]) {
      if (next < 0) continue;
      const q = point(next);
      if (!clear(p, q)) continue;
      const key = next * 2 + nextDirection;
      const cost =
        current.cost +
        Math.abs(p.x - q.x) +
        Math.abs(p.y - q.y) +
        (direction !== nextDirection ? 36 : 0);
      if (cost >= (costs.get(key) ?? Infinity)) continue;
      costs.set(key, cost);
      parents.set(key, current.key);
      queue.push({ key, cost, score: cost + Math.abs(q.x - b.x) + Math.abs(q.y - b.y) });
    }
  }
  if (found === undefined) return [start, a, { x: a.x, y: b.y }, b, end];
  const points: Point[] = [];
  for (let key: number | undefined = found; key !== undefined; key = parents.get(key))
    points.unshift(point(Math.floor(key / 2)));
  // Remove collinear grid steps before rounding the actual corners.
  const bends = points.filter(
    (p, i) =>
      !i ||
      i === points.length - 1 ||
      !(
        (points[i - 1].x === p.x && points[i + 1].x === p.x) ||
        (points[i - 1].y === p.y && points[i + 1].y === p.y)
      ),
  );
  return [start, ...bends, end];
}

export function anchoredPoints(points: Point[]): Point[] {
  if (points.length < 3) return points;
  const start = points[0],
    end = points[points.length - 1];
  const interior = points.slice(1, -1);
  const first = interior[0],
    last = interior[interior.length - 1];
  if (first.y !== start.y) interior.unshift({ x: first.x, y: start.y });
  if (last.y !== end.y) interior.push({ x: last.x, y: end.y });
  return [start, ...interior, end];
}

export function editablePoints(points: Point[]): Point[] {
  const bends: Point[] = [];
  const append = (point: Point) => {
    const last = bends[bends.length - 1];
    if (last && last.x === point.x && last.y === point.y) return;
    while (bends.length >= 2) {
      const a = bends[bends.length - 2],
        b = bends[bends.length - 1];
      if (!((a.x === b.x && b.x === point.x) || (a.y === b.y && b.y === point.y))) break;
      bends.pop();
    }
    const previous = bends[bends.length - 1];
    if (!previous || previous.x !== point.x || previous.y !== point.y) bends.push({ ...point });
  };
  for (const point of points) {
    const last = bends[bends.length - 1];
    // Older saved routes may contain a diagonal corner after a node was moved.
    // Restore a right angle first, then collapse retraced and collinear segments.
    if (last && last.x !== point.x && last.y !== point.y) append({ x: point.x, y: last.y });
    append(point);
  }
  if (bends.length < 2) return bends;
  const start = bends[0],
    end = bends[bends.length - 1];
  const next = bends[1],
    previous = bends[bends.length - 2];
  // Leave a short fixed stub at each socket. The rest of even the first/last
  // horizontal segment can then move vertically without moving the endpoint.
  const result = [...bends];
  if (start.y === next.y && Math.abs(next.x - start.x) > 48) {
    const stub = { x: start.x + Math.sign(next.x - start.x) * 24, y: start.y };
    result.splice(1, 0, stub, { ...stub });
  }
  if (previous.y === end.y && Math.abs(end.x - previous.x) > 48) {
    const stub = { x: end.x - Math.sign(end.x - previous.x) * 24, y: end.y };
    result.splice(result.length - 1, 0, stub, { ...stub });
  }
  return result;
}

export interface EdgeHandle extends Point {
  segment: number;
  axis: 'x' | 'y';
}
export function edgeHandles(points: Point[]): EdgeHandle[] {
  return points.slice(1, -2).flatMap((a, index) => {
    const segment = index + 1,
      b = points[segment + 1];
    if (Math.hypot(a.x - b.x, a.y - b.y) < 4) return [];
    return [
      {
        x: (a.x + b.x) / 2,
        y: (a.y + b.y) / 2,
        segment,
        axis: a.x === b.x ? ('x' as const) : ('y' as const),
      },
    ];
  });
}
