# Matriz de compatibilidade — IKP 0.1 e Scanner atual

Data: 2026-11-10  
Estado: P0 e P1 implementados; exportador IKP 0.1 integrado na CLI; projeção validada com captura real (8/21 kinds); validação do ZIP completo e importação na plataforma ainda pendentes.

## Conclusão

O Scanner atual já gera CaptureId, declara capabilities, regista coverage por operação, projeta o Object Index com SourceIdentity e fingerprints e exporta pacotes IKP 0.1 através de IkpPackageExporter. A CLI disponibiliza scan-ikp (captura + exportação) e export-ikp (exportação a partir de workspace).

O exportador constrói os cinco ficheiros de contrato obrigatórios, inclui artefactos referenciados, diagnósticos e factos REL/DTL/interpretations quando existem, calcula SHA-256 por ficheiro e digest lógico e escreve o ZIP atomicamente. Existem testes unitários dedicados ao exportador.

A projeção foi também validada com a captura real `CSDefault202603`: 16.618 objetos projetados, 8.307 SourceIdentity, 16.581 MetadataFingerprint e 49 artefactos com tamanho/hash confirmados. A captura contém apenas 8 dos 21 kinds mapeados; os restantes não ficam validados por esta evidência.

**Limite da confirmação atual:** a validação real incidiu na projeção do Object Index e nos artefactos da captura, não confirma ainda o ZIP completo produzido pelo exportador com o validador independente. A importação no Knowledge também não existe.

## Matriz

| Componente | Estado atual confirmado | Lacuna / validação restante |
|---|---|---|
| ScanManifest / CaptureId | CaptureId, capabilities, coverage, timestamps, modo e versão disponíveis durante o scan | Workspaces antigos podem não preservar todo o manifesto; manter metadados desconhecidos explícitos |
| CollectorResult / OperationCoverage | Cobertura por operação, resultado, disponibilidade, contagem e issues | Validar coerência para operações/kinds ainda não exercitados em captura real |
| ScanEngine / ScanMode | Full, Selective e ProbeOnly com semântica de cobertura | Executar suite atual e verificar CI |
| Object Index | Projeção read-only; validada com captura real para 8/21 kinds (16.618 objetos) | Validar 13 kinds não presentes e estabilidade entre capturas |
| Artefactos | 49 artefactos da captura real com byte length e SHA-256 confirmados na projeção/export preparation | Validar pacote ZIP completo e categorias adicionais |
| REL/DTL | Factos declarados e interpretações em payloads separados; valida contagens e referências | Confirmar payloads com a captura default e uma captura personalizada |
| IKP ZIP | IkpPackageExporter implementado; manifesto, capabilities, coverage, object-index, checksums e payload | Validar ZIP real com schemas e validador independente |
| CLI | scan-ikp e export-ikp integrados | Testar os dois caminhos com captura real |
| Validador independente | Schemas JSON e validador Python existem | Executar contra ZIP real, não apenas fixture sintética |
| PHC Knowledge | Ainda sem projetos em src/ e tests/ | Implementar receção, validação, arquivo e registo de importação |

## Próxima fatia de implementação

**PHC Knowledge — Receive / Validate / Archive**

1. Receber ZIP IKP por endpoint HTTP, com limites de tamanho e número de entradas.
2. Calcular SHA-256 dos bytes recebidos.
3. Validar estrutura ZIP e impedir path traversal, caminhos duplicados e entradas não declaradas.
4. Validar os cinco ficheiros obrigatórios, schemas, referências cruzadas e checksums.
5. Arquivar o ZIP original de forma imutável, fora da base de dados relacional.
6. Registar no SQL Server Express a tentativa de importação, PackageId, CaptureId, checksum, estado e diagnóstico.
7. Tornar a receção idempotente para o mesmo PackageId + checksum; rejeitar conflito de mesmo PackageId com conteúdo diferente.
8. Cobrir casos válidos, ZIP malicioso, checksum incorreto, contrato incompatível, duplicado idempotente e falha de arquivo.

A primeira fatia **não** associa automaticamente o pacote a uma instalação, não materializa entidades/observações, não calcula remoções e não publica conhecimento.

## Validação posterior do Scanner

1. Executar build e testes atuais.
2. Gerar IKP com a captura PHC default de referência já existente.
3. Validar o ZIP com tools/ikp-validation/validate_ikp_fixture.py.
4. Inspecionar contagens e payloads, sem inventar dados em falta.
5. Repetir com uma captura de instalação personalizada quando estiver disponível.

## Limites

- O Scanner continua a ser produtor de factos e pacotes; não recebe dependência runtime do Knowledge.
- O workspace SQLite não é lido diretamente pela plataforma.
- O pacote original é preservado; a base de dados central guarda metadados e estado da importação, não o ZIP como BLOB.
- Receber/validar/arquivar não equivale a associar, interpretar, materializar ou publicar conhecimento.
