# RpiWatcher

書籍『C#/.NET で動かす Raspberry Pi 入門』の題材アプリ。
ラズパイに付けた **LED 1個**で作る最小の「見張りランプ」。
アプリ自体が目的ではなく、**配置（デプロイ）と実機デバッグの型**を学ぶための器です。

- `System.Device.Gpio` で LED を点滅（Lチカ）
- 入力ピンを **内部プルアップ**にし、**押しボタンで GND につないだ瞬間**を検知してログ
- systemd で自動起動・常駐、`journalctl` でログ、停止時に **LED 消灯＋ピン解放**
- ログ文言は `Resources/*.resx` に分離（`ja` 既定、`en` 同梱＝英語版で流用）

## 必要なもの

- Raspberry Pi 2 以降（ARMv7+ / 64bit OS 推奨。**Pi Zero / Pi 1 は非対応**）
- LED × 1、抵抗 330Ω × 1、押しボタン（タクトスイッチ）× 1、ブレッドボード × 1、ジャンパー線数本
- 押しボタンが無ければ、入力はジャンパー線で GPIO24↔GND を一瞬つないでも代用できる

## 配線（既定ピン）

| 役割 | GPIO(BCM) | 物理ピン | つなぎ方 |
|---|---|---|---|
| LED | GPIO18 | 12番 | GPIO18 → LED(+) → 330Ω → GND |
| 入力 | GPIO24 | 18番 | GPIO24 → 押しボタン → GND（押すと検知） |

入力は **GPIO24＝物理18番**、隣の **物理20番＝GND**。押しボタンでこの2本をつなぐ
（ボタンが無ければジャンパー線で一瞬つないでも可）。ピンは `--led` / `--input` で変更可。

## 母艦だけで試す（実機なし・開発用）

```bash
dotnet run --project RpiWatcher -- --sim --verbose
```

`--sim` では LED 状態をログに出し、**Enter キーで入力**を代用します
（本編では実機の GPIO を扱います。これは母艦確認用の補助）。

## 実機へ配置する（3章：publish → 転送 → 実行）

**フレームワーク依存**で発行すると軽く、転送が速い（実機に .NET を一度だけ入れておく）。

実機に .NET を入れる（最初の1回だけ）:

```bash
# 実機（SSH先）
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel LTS
# ~/.bashrc に追記
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$HOME/.dotnet
```

配置:

```powershell
# 母艦（Windows）から一括で
./deploy/deploy.ps1 -PiHost pi@raspberrypi
```

手動なら:

```bash
dotnet publish RpiWatcher/RpiWatcher.csproj -c Release -o ./publish
scp -r ./publish pi@raspberrypi:~/rpiwatcher
ssh pi@raspberrypi "~/.dotnet/dotnet ~/rpiwatcher/RpiWatcher.dll"
```

> 実機に .NET を入れたくないときは自己完結型（`-r linux-arm64 --self-contained`、32bit は `linux-arm`）。転送は数十MBと重くなる。

## 実機を止めて見る（5章：リモートデバッグ・VS Code）

1. パスワードなし SSH を用意（`ssh-keygen -t ed25519` → `ssh-copy-id pi@raspberrypi`）
2. 実機に vsdbg を入れる

   ```bash
   curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l ~/vsdbg
   ```

3. **Debug で発行**して配置（pdb が要る）：`dotnet publish -c Debug -o ./publish` → `scp -r ./publish pi@raspberrypi:~/rpiwatcher`
4. `deploy/launch.sample.json` を `.vscode/launch.json` にコピー（`pipeArgs` のホストを自分の実機に）
5. `WatcherService.OnInput` にブレークポイント → **F5** → 実機でアプリが起動 → **ボタンを押す** → 停止して `count` を見る

> 常駐サービスを動かしているときは、先に `sudo systemctl stop rpiwatcher`（ピンの二重使用を避ける）。

## 動かし続ける（7章：自動起動・常駐・定期実行）

```bash
# サービス登録（deploy/rpiwatcher.service を配置）
sudo cp deploy/rpiwatcher.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now rpiwatcher

# ログを見る
journalctl -u rpiwatcher -f

# 停止（LED が消え、ピンが解放されることを確認）
sudo systemctl stop rpiwatcher
```

定期実行にしたいときは cron か systemd timer を使う（7章）。

## オプション

| 引数 | 既定 | 意味 |
|---|---|---|
| `--led N` | 18 | LED の GPIO 番号 |
| `--input N` | 24 | 入力の GPIO 番号 |
| `--interval MS` | 1000 | 点滅間隔（ミリ秒） |
| `--debounce MS` | 200 | デバウンス時間。連続入力を1回にまとめる |
| `--lang ja\|en` | ja | UI 言語（`RPIWATCHER_LANG` でも可） |
| `--sim` | off | 実機なしで動かす（開発用） |
| `--verbose` | off | 詳細ログ |

## よくある症状

- **1回の接触で複数回検知される** → チャタリング（スイッチのバウンス）。
  接点が数ミリ秒のあいだにオン/オフを繰り返すため。ソフトウェアデバウンスで
  一定時間内の連続エッジを1回にまとめている（既定200ms）。まだ多重検知するなら
  `--debounce` の値をさらに大きくする。押しボタンでもジャンパー線でも起こる。

## ライセンス / 注意

- **入力ピン ↔ GND は安全**。GPIO24 は内部プルアップ（約50kΩ）の入力なので、
  GND につないでも流れる電流は約66µA と微小。公式チュートリアルと同じ使い方。
- ただし **出力HIGH のピンや電源ピン（5V/3.3V）を GND に直結するのは厳禁**（短絡）。
  GPIO に **5V を入力**するのも不可（GPIO は 5V 非対応）。事故の多くは**ピンの数え間違い**。
- 配線の向き・抵抗値・入力モードを誤ると**基板や部品を壊しうる**。
  表のとおりに接続し、通電前に確認すること。
- .NET / Raspberry Pi OS 等のバージョン・手順は変わりうる。
  最新は公式（Microsoft Learn `.NET IoT`）を参照。
