#!/usr/bin/env python3

import argparse
import json
import os
import sys

project_root = os.path.dirname(os.path.dirname(__file__))

language_map = [
  # Copy pt-BR from APK to both pt-BR and pt-PT, because that's better than
  # nothing, and Valheim doesn't patch in similar language translations if
  # they're missing.
  ["brazilianportuguese", "Portuguese_Brazilian"],
  ["brazilianportuguese", "Portuguese_European"],

  ["chinesetraditional", "Chinese"],
  ["english", "English"],
  ["french", "French"],
  ["german", "German"],
  ["italian", "Italian"],
  ["japanese", "Japanese"],
  ["korean", "Korean"],
  ["russian", "Russian"],
  ["spanish", "Spanish"],
  ["thai", "Thai"],
]

def main():
  parser = argparse.ArgumentParser(description='Extract APK translations')
  parser.add_argument('--transfer-map', required=True,
      help='A JSON mapping of APK translation keys to our translation keys.')
  parser.add_argument('--output-file', required=True,
      help='The filename for the outputs.  For example, "giovanni.json".')
  parser.add_argument('--apk-path', required=True,
      help='The path to a folder with the dumped APK contents.')

  args = parser.parse_args()

  transfer_json = json.load(open(args.transfer_map, "r"))

  for srclang, dstlang in language_map:
    print("Transferring " + dstlang + " translations")

    dstpath = os.path.join(
        project_root,
        "Pokeheim",
        "Assets",
        "Translations",
        dstlang,
        args.output_file)

    srcpath = os.path.join(
        args.apk_path,
        "assets",
        "text",
        "i18n_" + srclang + ".json")

    try:
      dst = json.load(open(dstpath, "r"))
    except FileNotFoundError:
      dst = {}
    src = json.load(open(srcpath, "r"))
    src = dict(zip(src["data"][::2], src["data"][1::2]))

    for srckey, dstkey in transfer_json.items():
      print("  " + srckey + " => " + dstkey + " (" + src[srckey] + ")")
      dst[dstkey] = src[srckey]

    with open(dstpath, "w", encoding="utf8") as f:
      json.dump(dst, f, indent=2, ensure_ascii=False)
      f.write("\n")

if __name__ == '__main__':
  main()
