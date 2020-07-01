const outputPath = '/home/vic/.local/share/Steam/steamapps/compatdata/244850/pfx/drive_c/users/steamuser/Application Data/SpaceEngineers/IngameScripts/local'
const libraryPath = './library'

const name = process.argv[2]
const { readFileSync, writeFileSync, existsSync, mkdirSync } = require('fs')
const { basename, extname, join } = require('path')

const nameSpaces = {}

const scriptData = readFileSync(name, 'utf-8')
const scriptDataRows = scriptData.split('\n')

let minimumSpaces = Number.MAX_VALUE

const regExp = /\#region/
const endRegExp = /\#endregion/

let open = false

const scriptFilteredRows = scriptDataRows.filter(e => {
  let now = open

  if (regExp.test(e))
    now = open = true

  if (endRegExp.test(e))
    open = false

  return !now
})

for (let row of scriptFilteredRows) {
  let i = 0;

  if (row.length == 0)
    continue

  while (/\s+/i.test(row[i])) 
    i++
  
  if (i < minimumSpaces)
    minimumSpaces = i
}

const scriptForGame = scriptFilteredRows.map(e => e.substr(minimumSpaces)).join('\n')
const scriptName = basename(name, extname(name))
const outputFolder = join(outputPath, 'WSV-'+scriptName+'Script')

if(!existsSync(outputFolder))
  mkdirSync(outputFolder)

writeFileSync(join(outputFolder, 'Script.cs'), scriptForGame.trim())