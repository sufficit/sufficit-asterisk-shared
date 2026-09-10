# Limites opcionais do socket — 2026-09-09 18:25 -03

AISingleSocketHandler agora aceita fila limitada sem espera do consumidor e limite
de caracteres da linha, inclusive fragmentos sem terminador. Permite CR final quando
CRLF chega fragmentado. Saturação/linha excessiva fecha socket com flag
ReceiveLimitExceeded, sem registrar conteúdo. Manager interpreta como falha terminal
da geração; nova instância é responsabilidade do supervisor.

Defaults zero preservam AGI e usuários antigos. BufferSize limita alocação por leitura;
fila e linha limitadas não significam limite global do processo/aplicação.
Validado com TCP simulado; versões net7/net10/netstandard2.0 compiladas via Manager.
Código local não publicado em serviços existentes.

[Contrato e testes](../../sufficit-asterisk-manager/Documentation/202609091825-bounded-ami-receive.md).
