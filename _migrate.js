const fs = require("fs");
const path = require("path");
import { createHash } from "crypto";

const OLD_VOICE_DATA_PATH = "/stuff/symlink/XIV_Voices/Data"; // voice folder
const OLD_PLUGIN_DATA_PATH = "/stuff/code/XivVoices/XivVoices/Data"; // source code folder containing voiceNames.json, etc.
const NEW_DATA_PATH = "/stuff/code/XivVoices-WIP/_data"; // output migration folder

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

fs.writeFileSync(
  path.join(NEW_DATA_PATH, "manifest.json"),
  JSON.stringify(manifest, null, 2),
);

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
        const json = JSON.parse(
          fs.readFileSync(path.join(OLD_VOICE_DATA_PATH, voice, speaker, file)),
        );
        const oggPath = path.join(
          OLD_VOICE_DATA_PATH,
          voice,
          speaker,
          file.replace(".json", ".ogg"),
        );
        if (fs.existsSync(oggPath)) {
          const newFileName =
            sha256(voice, json.speaker, json.sentence) + ".ogg";
          const newFilePath = path.join(NEW_DATA_PATH, newFileName);
          fs.copyFileSync(oggPath, newFilePath);
        }
      }
    });
  });
});
