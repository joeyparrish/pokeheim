#!/usr/bin/env python3

import json
import os
import sys

def load_translations(language):
  path = "Pokeheim/Assets/Translations/" + language

  d = {}
  for json_filename in os.listdir(path):
    json_path = os.path.join(path, json_filename)
    d[json_filename] = json.load(open(json_path, "r"))

  return d

all_keys = {}

for language in os.listdir("Pokeheim/Assets/Translations"):
  translations = load_translations(language)

  for filename in translations.keys():
    if filename not in all_keys:
      all_keys[filename] = set()

    for key in translations[filename]:
      all_keys[filename].add(key)

num_keys = 0
for filename in all_keys:
  num_keys += len(all_keys[filename])

for language in os.listdir("Pokeheim/Assets/Translations"):
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
  print("*** {} translation is {:.1f}% complete. ***".format(language, completeness))
