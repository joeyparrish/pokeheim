#!/usr/bin/env python3

import json
import os
import sys

def shown_languages():
  if len(sys.argv) > 1:
    return [sys.argv[1]]
  return all_languages()

def all_languages():
  return os.listdir("Pokeheim/Assets/Translations")

def load_translations(language):
  path = "Pokeheim/Assets/Translations/" + language

  d = {}
  for json_filename in os.listdir(path):
    # New language names only appear in the English localizations, so don't
    # count them.
    if json_filename == "language.json":
      continue

    json_path = os.path.join(path, json_filename)
    d[json_filename] = json.load(open(json_path, "r"))

  return d

all_keys = {}

for language in all_languages():
  translations = load_translations(language)

  for filename in translations.keys():
    if filename not in all_keys:
      all_keys[filename] = set()

    for key in translations[filename]:
      all_keys[filename].add(key)

num_keys = 0
for filename in all_keys:
  num_keys += len(all_keys[filename])


summary = {}

for language in shown_languages():
  print("{}\n=====".format(language))
  translations = load_translations(language)
  completeness = 0

  for filename in all_keys:
    if filename not in translations:
      print("Missing all {} translations from {}".format(language, filename))
      continue

    for key in all_keys[filename]:
      if key in translations[filename]:
        completeness += 1
      else:
        print("Missing {} translation for {} in {}".format(language, key, filename))

  completeness = 100.0 * completeness / num_keys
  summary[language] = completeness

  print("{} translation is {:.1f}% complete".format(language, completeness))
  print()

print("Summary:")
for language in shown_languages():
  print("  {}: {:.1f}% complete".format(language, summary[language]))

