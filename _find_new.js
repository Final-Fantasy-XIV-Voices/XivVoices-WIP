const fs = require("fs");
const path = require("path");

const SEARCHTERM = "is the reward lacking?";

const PATH = "/stuff/code/_xivv_data/voices";

let count = 0;
const files = fs.readdirSync(PATH);
files.forEach((file) => {
  if (!file.endsWith(".json")) return;
  const jsonPath = path.join(PATH, file);
  const json = JSON.parse(fs.readFileSync(jsonPath));
  if (json.Sentence.toLowerCase().includes(SEARCHTERM.toLowerCase())) {
    console.log(jsonPath.replace(".json", ".ogg"), json);
    count++;
  }
});
console.log("Count: " + count);
