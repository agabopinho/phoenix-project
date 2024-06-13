from asyncio import sleep
import asyncio
import subprocess
import sys


def output_reader(proc, file):
    while True:
        byte = proc.stdout.read(1)
        if byte:
            sys.stdout.buffer.write(byte)
            sys.stdout.flush()
            file.buffer.write(byte)
        else:
            break


if __name__ == "__main__":
    for port in sys.argv[1:]:
        sp = subprocess.Popen(f"python main.py {port}")
    while True:
        asyncio.get_event_loop().run_until_complete(sleep(1000))
