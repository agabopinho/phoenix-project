from asyncio import sleep
import asyncio
import subprocess
import sys


if __name__ == "__main__":
    for portSetting in sys.argv[1:]:
        value = portSetting.split("+")
        port = int(value[0])
        quantity = int(value[1]) if len(value) > 1 else 0
        quantity += 1
        for i in range(quantity):
            sp = subprocess.Popen(f"python main.py {port}")
            port += 1
    while True:
        asyncio.get_event_loop().run_until_complete(sleep(1))
