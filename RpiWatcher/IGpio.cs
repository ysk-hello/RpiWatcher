namespace RpiWatcher;

// LED 出力と入力イベントだけを持つ最小の窓口。
// 実機用（RealGpio）と開発用（SimGpio）を
// 同じ形で扱えるようにする。
internal interface IGpio : IDisposable
{
    // 入力を検知したとき発火する。
    // 注意: 実機では別スレッドから呼ばれる。
    event Action? InputTriggered;

    // ピンを開く。実機で失敗すると例外。
    void Start();

    // LED を点ける／消す。
    void SetLed(bool on);
}
