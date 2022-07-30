import json
import time
from os import path, system

from settings import *

rootdir = "..\\market_analyzer\\ConsoleApp\\bin\\debug\\net6.0\\"

appSettingsPath = path.join(rootdir, "appsettings.json")
appPath = path.join(
    rootdir,
)

jsonData = json.loads(open(appSettingsPath).read())

for d in DATES[INDEX]:
    jsonData["Operation"]["Date"] = d.strftime("%Y-%m-%d")

    with open(appSettingsPath, "w") as file:
        file.truncate()
        file.write(json.dumps(jsonData))

    system("cd {} && start {}".format(rootdir, "ConsoleApp.exe"))
    time.sleep(1.5)
