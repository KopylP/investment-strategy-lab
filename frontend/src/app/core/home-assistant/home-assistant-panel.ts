const ingressPath = '/api/hassio_ingress/';

export function isInHomeAssistantPanel(document: Document): boolean {
  return document.baseURI.includes(ingressPath);
}
