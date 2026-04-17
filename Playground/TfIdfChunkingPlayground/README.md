# TF-IDF Chunking Playground

This is a small .NET 8 console project for testing the existing `Services.AI.Text` TF-IDF and hybrid chunking implementation against a `.docx` file.

Default document:

- `C:\Users\chuon\Downloads\SUỘC VNR – 395 câu.docx`

## Run

From `capstone_be2/Playground/TfIdfChunkingPlayground/`:

```powershell
Set-Location .\Playground\TfIdfChunkingPlayground
dotnet run
```

Or specify a different file and options:

```powershell
Set-Location .\Playground\TfIdfChunkingPlayground
dotnet run -- \
  --file "C:\Users\chuon\Downloads\SUỘC VNR – 395 câu.docx" \
  --min-chunk-tokens 120 \
  --max-chunk-tokens 220 \
  --overlap-sentences 1 \
  --similarity-window-sentences 3 \
  --similarity-drop-threshold 0.2
```

## Output

The app writes reports under `bin\Debug\net8.0\output\`:

- `summary.txt`: run settings, extracted size, chunk count, vocabulary size
- `chunks.txt`: every chunk, top TF-IDF terms, and chunk text
- `similarity.txt`: nearest chunk neighbors by cosine similarity

## Notes

- This project links the existing text-processing source files instead of copying them.
- TF-IDF is built across the produced chunks, so each chunk is treated as a document for scoring.
