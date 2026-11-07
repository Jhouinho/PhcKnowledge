# Validador local IKP 0.1

Validador de referência para um IKP já extraído. Aplica os JSON Schemas Draft 2020-12 e valida integridade, cobertura e referências cruzadas entre os ficheiros.

## Requisitos

- Python 3.10+
- Dependência: `jsonschema` (instalar com `python -m pip install -r tools/ikp-validation/requirements.txt`)

## Validar a fixture

A partir da raiz do repositório:

```bash
python tools/ikp-validation/validate_ikp_fixture.py docs/architecture/fixtures/ikp-0.1/minimal-valid
```

## Validar a saída real do projector C#

O workflow .NET guarda os bytes de um `object-index.json` produzido pelo `ObjectIndexProjector` a partir de uma captura sintética. Em seguida, executa o validador Draft 2020-12 oficial:

```bash
python tools/ikp-validation/validate_object_index.py caminho/para/object-index.json
```

Isto valida os bytes emitidos pelo código C#, sem duplicar o JSON Schema no projector.

## Executar testes positivos e negativos

```bash
python -m pip install -r tools/ikp-validation/requirements.txt
python -m unittest discover -s tools/ikp-validation -p "test_*.py" -v
```

Os testes trabalham sobre cópias temporárias da fixture e nunca alteram os ficheiros de referência.

## Âmbito

Valida os cinco ficheiros de contrato, os artefactos declarados, os checksums SHA-256, o digest lógico, CaptureId, capabilities, coverage, objetos, relações e issues.

Não valida ainda o ZIP binário, limites anti-zip-bomb, política de segurança/privacidade, assinatura digital, nem a compatibilidade semântica entre versões de capabilities. Essas verificações pertencem ao validador de ingestão da plataforma.
