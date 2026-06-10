New-Item -ItemType Directory -Force -Path ".act/artifacts" | Out-Null

act pull_request `
    -W .github/workflows/main.yaml `
    -j test `
    --artifact-server-path .act/artifacts `
    -e .act/events/pull_request.json
