const outputPath = '/home/vic/.local/share/Steam/steamapps/compatdata/244850/pfx/drive_c/users/steamuser/Application Data/SpaceEngineers/IngameScripts/local'
const excludeSearch = [
  '.vscode',
  'bin',
  'Bin64',
  'obj'
]

/** @type {{[key: string]: {usedin: string[], required: string[], code: string}}} */
const usingNamespaces = {}
const name = process.argv[2]
const { readFileSync, writeFileSync, existsSync, mkdirSync, readdirSync, statSync } = require('fs')
const { basename, extname, join } = require('path')

function getNamespaceObject(name = '') {
  return usingNamespaces[name] || 
    (usingNamespaces[name] = {usedin: [], code: '', required: []})
}

function normalizeScript(scriptData = '', removeRegions = false) {
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

  
    return removeRegions ? !now : !(regExp.test(e) || endRegExp.test(e))
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

  return scriptFilteredRows.map(e => e.substr(minimumSpaces)).join('\n')
}


/**
 * @param {string} scriptData 
 */
function getNameSpaces(scriptData = '') {
  /** @type {{[key: string]: string}} */
  const data = {}
  const regex = /namespace\s+([^{]+){/m

  /** @type {RegExpExecArray} */
  let array = null
  
  while(array = regex.exec(scriptData)) {
    const regExp = /[\{|\}]/
    let inputs = 0, startIndex = array.index + array[0].length
    let namespaceName = array[1].trim()

    for(let i = startIndex; i < scriptData.length; i++) {
      if(!regExp.test(scriptData[i]))
        continue

      let open = scriptData[i] == '{' ? true : false

      if(open) inputs++
      else inputs--

      if(inputs < 0) {
        const scriptDataOut = scriptData.substr(startIndex, i - 1 - startIndex)

        if(!data[namespaceName]) 
          data[namespaceName] = ''

        data[namespaceName] += '\n' + normalizeScript(scriptDataOut)
        break
      }
    }

    scriptData = scriptData.replace(array[0], '')
  }

  return data
}

function getUsing(scriptData = '') {
  /** @type {string[]} */
  const usingNamespaces = []  
  const regex = /using\s+([^;]+)\s*;/gm;
  
  /** @type {RegExpExecArray} */
  let array = null
  let findingData = scriptData

  while(array = regex.exec(findingData)) {
    findingData.replace(array[0], '')

    if(usingNamespaces.indexOf(array[1]) == -1)
      usingNamespaces.push(array[1])
  }
  
  return usingNamespaces
}

function readData(fileName = '') {
  return readFileSync(fileName, 'utf-8')
}

function scanDir(dir = process.cwd()) {
  const tasks = [dir]
  const files = ['']

  files.pop()

  while(tasks.length) {
    let now = tasks.shift()

    for(let dir of readdirSync(now)) {
      let newDir = join(now, dir)

      if(statSync(newDir).isDirectory() && excludeSearch.indexOf(newDir) == -1)
        tasks.push(newDir)
      
      if(/\.cs$/.test(newDir)) {
        let data = readData(newDir)
        let using = getUsing(data)
        let namespaces = getNameSpaces(data)

        for(let name in namespaces) {
          let b = getNamespaceObject(name)
          b.code += namespaces[name]
        }

        for(let name of using) {
          let d = getNamespaceObject(name)

          for(let used in namespaces) {
            let b = getNamespaceObject(used)

            if(b.required.indexOf(used) == -1)
              b.required.push(name)

            if(d.usedin.indexOf(used) == -1)
              d.usedin.push(used)
          }
        }
      }
    }
  }
}

excludeSearch.map((e, i, d) => d[i] = join(process.cwd(), e))

scanDir()

/** @type {{[key: string]: string}} */
const imports = {}
const scriptData = readData(name)
const using = getUsing(scriptData)

/** @param {string[]} using */
function loadImports(using = []) {
  for(let used of using) {
    let object = getNamespaceObject(used)

    if(!object.code) 
      continue

    if(imports[used])
      continue

    imports[used] = object.code

    if(object.required.length)
      loadImports(object.required)
  }
}

loadImports(using)

const outputData = [
  normalizeScript(scriptData, true)
]

for(let name in imports) {
  outputData.push('//namespace ' + name + '\n' + imports[name].trim())
}

outputData.map((e, i, d) => d[i] = e.trim())

const scriptName = basename(name, extname(name))
const outputFolder = join(outputPath, 'WSV-'+scriptName+'Script')

if(!existsSync(outputFolder))
  mkdirSync(outputFolder)

writeFileSync(join(outputFolder, 'Script.cs'), outputData.join('\n\n').trim())

