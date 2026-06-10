act push `
    -W .github/workflows/main.yaml `
    -j test `
    -e .act/events/push-ci.json
