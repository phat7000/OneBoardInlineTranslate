"""Private JSON-lines worker for OneBoard's local translation provider.

The worker is launched only by OneBoard. User text is read from stdin and
returned on stdout; it is never written to a file or included in error output.
"""

import json
import os
import sys
from collections import OrderedDict

import ctranslate2
import sentencepiece


MAX_LOADED_MODELS = max(1, min(3, int(os.environ.get("ONEBOARD_MAX_MODELS", "2"))))
_models = OrderedDict()


def _load_model(model_path):
    normalized = os.path.abspath(model_path)
    cached = _models.pop(normalized, None)
    if cached is not None:
        _models[normalized] = cached
        return cached

    model_directory = os.path.join(normalized, "model")
    tokenizer_path = os.path.join(normalized, "sentencepiece.model")
    translator = ctranslate2.Translator(
        model_directory,
        device="cpu",
        compute_type="int8",
        inter_threads=1,
        intra_threads=0,
    )
    tokenizer = sentencepiece.SentencePieceProcessor(model_file=tokenizer_path)
    loaded = (translator, tokenizer)
    _models[normalized] = loaded
    while len(_models) > MAX_LOADED_MODELS:
        _models.popitem(last=False)
    return loaded


def _translate_text(model_path, text):
    translator, tokenizer = _load_model(model_path)
    lines = text.splitlines(keepends=True)
    if not lines:
        lines = [text]

    translated = []
    for line in lines:
        content = line.rstrip("\r\n")
        ending = line[len(content):]
        if not content.strip():
            translated.append(content + ending)
            continue
        tokens = tokenizer.encode(content, out_type=str)
        result = translator.translate_batch(
            [tokens],
            beam_size=4,
            length_penalty=0.2,
            max_decoding_length=512,
            replace_unknowns=True,
        )[0]
        decoded = (
            tokenizer.decode_pieces(result.hypotheses[0])
            .replace("▁", " ")
            .replace("_", " ")
            .lstrip()
        )
        translated.append(decoded + ending)
    return "".join(translated)


def _respond(value):
    sys.stdout.write(json.dumps(value, ensure_ascii=False, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def main():
    for raw_line in sys.stdin:
        request_id = None
        try:
            request = json.loads(raw_line)
            request_id = request.get("id")
            model_path = request["modelPath"]
            text = request["text"]
            if not isinstance(text, str) or not text.strip():
                raise ValueError("empty input")
            result = _translate_text(model_path, text)
            _respond({"id": request_id, "ok": True, "text": result})
        except Exception as error:  # Deliberately redact messages and input.
            _respond({"id": request_id, "ok": False, "error": type(error).__name__})


if __name__ == "__main__":
    main()
