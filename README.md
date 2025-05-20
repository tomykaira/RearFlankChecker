# RearFlankChecker
FFXIV向けのACTプラグインです。

方向指定条件のあるスキル実行時にミスしたら、音で知らせたりミスしたスキルの情報を表示します。  
`
低レベル時、または低レベルにシンクされた状態だとチェックが正しく行われない場合があります。
最大レベルでのコンテンツで動作確認してます。
`
## ダウンロード
[リリースページ](https://github.com/furutto/RearFlankChecker/releases/latest)からダウンロード
## インストール
1. ダウンロードファイルを任意のディレクトリに展開 
1. resources フォルダをACT本体があるディレクトリかRearFlankChecker.dllと同じディレクトリに配置
1. RearFlankChecker.dll を ACT からプラグイン追加
#### 動作環境
* .NET Framework 4.8 以降
## 使用方法
![Config](https://github.com/furutto/RearFlankChecker/blob/master/resources/image/readme_config.jpg)
1. [Plugins]タブの[Rear And Flank Checker]タブに設定項目があります。
1. 方向指定ミス時に鳴らす効果音の設定
1. 方向指定ミスの情報を表示するビューアに関する設定
![Viewer](https://github.com/furutto/RearFlankChecker/blob/master/resources/image/readme_viewer.jpg)
1. ミスした場合にスキル名と回数を表示
## ライセンス
[三条項BSDライセンス](LICENSE)  
## 連絡先
twitter:  [@furutto_dev](https://twitter.com/furutto_dev)  

## resources/potencies.json の更新方法

[xivanalysis](https://github.com/xivanalysis/xivanalysis)のコードをダウンロードします。
ルートディレクトリに `export.ts` を作成し、 `npx ts-node export.ts` で実行します。

```
import { ACTIONS } from './src/data/ACTIONS';
import { Potency } from './src/data/ACTIONS/type';
import fs from "fs";

const out: { [key: string]: { name: string, potencies: Potency[] } } = {};
for (const k of Object.keys(ACTIONS) as Array<keyof typeof ACTIONS>) {
    const v = ACTIONS[k];
    if (v.potencies) {
        out[v.id] = { name: v.name, potencies: v.potencies };
    }
}

const jsonString = JSON.stringify(out, null, 2);

fs.writeFileSync("potencies.json", jsonString, 'utf8');
```
