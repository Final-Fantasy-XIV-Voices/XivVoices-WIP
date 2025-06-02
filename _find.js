const fs = require("fs");
const path = require("path");

const SEARCHTERM = "ñ";

const OLD_VOICE_DATA_PATH = "/stuff/symlink/XIV_Voices-orig/Data"; // voice folder

let count = 0;
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

        if (json.speaker.toLowerCase().includes(SEARCHTERM.toLowerCase())) {
          count++;

          console.log(
            path.join(
              OLD_VOICE_DATA_PATH,
              voice,
              speaker,
              file.replace("json", "ogg"),
            ),
            json,
          );
        }
      }
    });
  });
});

console.log("Count: " + count);
