const fs = require("fs");
const path = require("path");

const SEARCHTERM = "“”";

const OLD_VOICE_DATA_PATH = "/stuff/symlink/XIV_Voices/Data"; // voice folder

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
        const json = JSON.parse(
          fs.readFileSync(path.join(OLD_VOICE_DATA_PATH, voice, speaker, file)),
        );

        if (json.sentence.includes(SEARCHTERM)) {
          count++;
          console.log(json);
        }
      }
    });
  });
});

console.log("Count: " + count);
