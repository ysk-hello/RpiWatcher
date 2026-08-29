# RpiWatcher

The subject app for the book *Raspberry Pi with C# and .NET*.
A minimal "watcher lamp" built from a **single LED** on the Pi.
The app itself isn't the point — it's a vessel for learning the
**deploy-and-on-device-debug pattern**.

- Blink an LED (Blinky) with `System.Device.Gpio`
- Set the input pin to **internal pull-up** and log the moment a
  **push button pulls it to GND**
- Auto-start and stay resident with systemd, read logs with
  `journalctl`, and **turn the LED off + release the pins** on stop
- Log messages live in `Resources/*.resx` (`ja` default, `en`
  bundled = reused by the English edition)

## What you need

- Raspberry Pi 2 or later (ARMv7+ / 64-bit OS recommended.
  **Pi Zero / Pi 1 are not supported**)
- 1 LED, 1 × 330Ω resistor, 1 push button (tact switch),
  1 breadboard, a few jumper wires
- No push button? You can stand in for the input by briefly
  touching GPIO24 to GND with a jumper wire.

## Wiring (default pins)

| Role | GPIO (BCM) | Physical pin | Connection |
|---|---|---|---|
| LED | GPIO18 | pin 12 | GPIO18 → LED(+) → 330Ω → GND |
| Input | GPIO24 | pin 18 | GPIO24 → push button → GND (press = detect) |

Input is **GPIO24 = physical pin 18**, with the neighboring
**physical pin 20 = GND**. A push button bridges these two
(a jumper wire touched briefly works too). Change the pins with
`--led` / `--input`.

## Try it on the dev machine (no hardware, for development)

```bash
dotnet run --project RpiWatcher -- --sim --verbose
```

With `--sim`, LED state is written to the log and **the Enter key
stands in for the input** (the book works with real GPIO on the
device; this is just a helper for checking things on your PC).

## Deploy to the device (Chapter 3: publish → transfer → run)

Publishing **framework-dependent** keeps the payload small and the
transfer fast (install .NET on the device once, up front).

Install .NET on the device (one time only):

```bash
# On the device (over SSH)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel LTS
# Add to ~/.bashrc
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet
```

Deploy:

```powershell
# From the dev machine (Windows), all in one step
./deploy/deploy.ps1 -PiHost pi@raspberrypi
```

Or manually:

```bash
dotnet publish RpiWatcher/RpiWatcher.csproj -c Release -o ./publish
scp -r ./publish pi@raspberrypi:~/rpiwatcher
ssh pi@raspberrypi "~/.dotnet/dotnet ~/rpiwatcher/RpiWatcher.dll"
```

> If you'd rather not install .NET on the device, publish
> self-contained (`-r linux-arm64 --self-contained`, or `linux-arm`
> for 32-bit). The transfer grows to tens of MB.

## Stop it on the device and look (Chapter 5: remote debugging, VS Code)

1. Set up passwordless SSH (`ssh-keygen -t ed25519` →
   `ssh-copy-id pi@raspberrypi`)
2. Install vsdbg on the device

   ```bash
   curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l ~/vsdbg
   ```

3. **Publish in Debug** and deploy (you need the pdb):
   `dotnet publish -c Debug -o ./publish` →
   `scp -r ./publish pi@raspberrypi:~/rpiwatcher`
4. Copy `deploy/launch.sample.json` to `.vscode/launch.json`
   (set the host in `pipeArgs` to your device)
5. Set a breakpoint on `WatcherService.OnInput` → **F5** → the app
   starts on the device → **press the button** → it breaks and you
   can inspect `count`

> If the resident service is running, stop it first with
> `sudo systemctl stop rpiwatcher` (to avoid using the pins twice).

## Keep it running (Chapter 7: auto-start, residency, scheduling)

```bash
# Register the service (deploy/rpiwatcher.service)
sudo cp deploy/rpiwatcher.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now rpiwatcher

# Watch the logs
journalctl -u rpiwatcher -f

# Stop it (confirm the LED goes off and the pins are released)
sudo systemctl stop rpiwatcher
```

For scheduled runs, use cron or a systemd timer (Chapter 7).

## Options

| Argument | Default | Meaning |
|---|---|---|
| `--led N` | 18 | GPIO number for the LED |
| `--input N` | 24 | GPIO number for the input |
| `--interval MS` | 1000 | Blink interval (ms) |
| `--debounce MS` | 200 | Debounce time; collapses repeated inputs into one |
| `--lang ja\|en` | ja | UI language (`RPIWATCHER_LANG` also works) |
| `--sim` | off | Run without hardware (for development) |
| `--verbose` | off | Verbose logs |

## Common symptoms

- **One press is detected several times** → contact bounce (switch
  bounce). The contacts flip on/off over a few milliseconds. A
  software debounce collapses the repeated edges within a set
  window into one (default 200 ms). If it still multi-triggers,
  raise `--debounce`. It happens with both a push button and a
  jumper wire.

## License / cautions

- **Input pin ↔ GND is safe.** GPIO24 is an input with internal
  pull-up (~50kΩ), so touching it to GND draws only about 66 µA —
  the same usage as the official tutorial.
- But **never tie an output-HIGH pin or a power pin (5V/3.3V)
  directly to GND** (a short). And **do not feed 5V into a GPIO**
  (GPIO is not 5V-tolerant). Most accidents come from
  **miscounting pins**.
- Wrong polarity, resistor value, or input mode can **damage the
  board or parts**. Wire it exactly as the table shows and
  double-check before powering on.
- .NET, Raspberry Pi OS, and the steps here can change over time.
  For the latest, see the official docs (Microsoft Learn
  `.NET IoT`).
