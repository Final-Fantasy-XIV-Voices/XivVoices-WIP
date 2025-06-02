const fs = require("fs");
const path = require("path");
import { createHash } from "crypto";

const OLD_VOICE_DATA_PATH = "/stuff/symlink/XIV_Voices/Data"; // voice folder
const OLD_PLUGIN_DATA_PATH = "/stuff/code/XivVoices/XivVoices/Data"; // source code folder containing voiceNames.json, etc.
const NEW_DATA_PATH = "/stuff/code/XivVoices-WIP/_data"; // output migration folder
const NEW_MANIFEST_PATH = path.join(NEW_DATA_PATH, "manifest.json");
const NEW_VOICES_PATH = path.join(NEW_DATA_PATH, "voices");

if (!fs.existsSync(NEW_DATA_PATH)) fs.mkdirSync(NEW_DATA_PATH);
if (!fs.existsSync(NEW_VOICES_PATH)) fs.mkdirSync(NEW_VOICES_PATH);

const RomanNumberDictionary = {
  I: 1,
  V: 5,
  X: 10,
  L: 50,
  C: 100,
  D: 500,
  M: 1000,
};

const NumberRomanDictionary = {
  1000: "M",
  900: "CM",
  500: "D",
  400: "CD",
  100: "C",
  90: "XC",
  50: "L",
  40: "XL",
  10: "X",
  9: "IX",
  5: "V",
  4: "IV",
  1: "I",
};

const old_ignored = JSON.parse(
  fs.readFileSync(path.join(OLD_PLUGIN_DATA_PATH, "ignored.json")),
);
const old_npc_data = JSON.parse(
  fs.readFileSync(path.join(OLD_PLUGIN_DATA_PATH, "npcData.json")),
);
const old_nameless = JSON.parse(
  fs.readFileSync(path.join(OLD_PLUGIN_DATA_PATH, "nameless.json")),
);
const old_voice_names = JSON.parse(
  fs.readFileSync(path.join(OLD_PLUGIN_DATA_PATH, "voiceNames.json")),
);

const manifest = {
  IgnoredSpeakers: old_ignored,
  Voices: [],
  Nameless: old_nameless,
  NpcData: old_npc_data,
};

old_voice_names.forEach((e) => {
  manifest.Voices.push({ Name: e.voiceName, Speakers: e.speakers });
});

fs.writeFileSync(NEW_MANIFEST_PATH, JSON.stringify(manifest, null, 2));

function sha256(...inputs) {
  const combinedInput = inputs.join(":");
  const hash = createHash("sha256");
  hash.update(combinedInput, "utf8");
  return hash.digest("hex");
}

const voices = fs.readdirSync(OLD_VOICE_DATA_PATH);
voices.forEach((voice) => {
  const speakers = fs.readdirSync(path.join(OLD_VOICE_DATA_PATH, voice));
  speakers.forEach((speaker) => {
    const files = fs.readdirSync(
      path.join(OLD_VOICE_DATA_PATH, voice, speaker),
    );
    files.forEach((file) => {
      if (file.endsWith(".json")) {
        const jsonPath = path.join(OLD_VOICE_DATA_PATH, voice, speaker, file);
        const json = JSON.parse(fs.readFileSync(jsonPath));
        const oggPath = path.join(
          OLD_VOICE_DATA_PATH,
          voice,
          speaker,
          file.replace(".json", ".ogg"),
        );
        if (fs.existsSync(oggPath)) {
          const newFileName = sha256(
            voice,
            ReplaceSpeaker(json.speaker),
            ReplaceSentence(json.sentence),
          );
          const newOggPath = path.join(NEW_VOICES_PATH, newFileName + ".ogg");
          fs.copyFileSync(oggPath, newOggPath);
          const newJsonPath = path.join(NEW_VOICES_PATH, newFileName + ".json");
          fs.writeFileSync(
            newJsonPath,
            JSON.stringify(
              {
                Speaker: ReplaceSpeaker(json.speaker),
                Sentence: ReplaceSentence(json.sentence),
                Voice: voice,
              },
              null,
              2,
            ),
          );
        }
      }
    });
  });
});

function ReplaceSpeaker(speaker) {
  if (speaker.endsWith("'s Voice")) speaker = speaker.replace("'s Voice", "");
  if (speaker != "???")
    speaker = speaker.replaceAll("!", "").replaceAll("?", "");

  return speaker;
}

// we have 1037 lines that include "arc" and the voicelines themselves are perfectly fucking fine. do not throw them away??
// and no, they do not say "arc" but instead the generic "adventurer" replacement. maybe a very few do say "arc" but whatever.c
// This also replaces the few _FIRSTNAME_ and _LASTNAME_ instances to just _NAME_
function ReplaceSentence(sentence) {
  // bruh one says "Arc of the Worthy" ???
  if (sentence.includes("Arc of the Worthy")) return sentence;
  // idek about this one
  if (sentence.includes("Sixth Arc Era")) return sentence;
  sentence = sentence.replace("_FIRSTNAME_", "_NAME_");
  sentence = sentence.replace("_LASTNAME_", "_NAME_");

  if (sentence.startsWith("...")) sentence = sentence.slice(3);

  sentence = sentence.replace(/(\.{3})(\w)/g, "$1 $2");

  sentence = convertRomanNumerals(sentence);

  sentence = sentence.replace(/\bArc\b(?='s)?/g, (match) => {
    return match === "Arc" ? "_NAME_" : "_NAME_'s";
  });

  sentence = sentence.replace(/<[^<]*>/g, "");

  sentence = sentence.replace(/\s+/g, " ").trim();

  return sentence;
}

function romanTo(number) {
  let roman = "";
  for (const key of Object.keys(NumberRomanDictionary)
    .map(Number)
    .sort((a, b) => b - a)) {
    while (number >= key) {
      roman += NumberRomanDictionary[key];
      number -= key;
    }
  }
  return roman;
}

function romanFrom(roman) {
  let total = 0;
  let previous = 0;

  for (let i = 0; i < roman.length; i++) {
    const current = RomanNumberDictionary[roman[i]];
    if (previous && current > previous) {
      total = total - 2 * previous + current;
    } else {
      total += current;
    }
    previous = current;
  }

  return total;
}

function convertRomanNumerals(text) {
  let value = text;
  for (let i = 25; i > 5; i--) {
    const numeral = romanTo(i);
    if (numeral.length > 1) {
      value = value.replace(new RegExp(numeral, "g"), i.toString());
    }
  }
  return value;
}
