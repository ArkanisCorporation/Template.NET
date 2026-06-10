New-Item -ItemType Directory -Force -Path ".act/artifacts" | Out-Null

act push `
    -W .github/workflows/main.yaml `
    -j test `
    --artifact-server-path .act/artifacts `
    -e .act/events/push-ci.json
