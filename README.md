# Re:VIEWのファイルをBloggerへ流し込めるようにするやつ

## 機能

https://github.com/muojp/review-blog-template/ のテンプレに従って記述した.reファイル(Re:VIEWドキュメント)をBloggerへ流し込むのに適した形式へと変換します。

footnoteに付与するid属性を記事横断でユニークにしたり、Blogger editorのURI変換によってリンクが壊れがちな箇所を回避したり、という機能を持ちます。

## 必要なもの(確認済み環境)

- Mac OS X 10.11.6
- .NET Core 1.0
- review-compile 1.6.0 (2.0.0の仕様変更によって図版の採番機能が動作しないことがわかっています)
 
## インストール方法

### project.jsonファイルを作成

Re:VIEWのarticlesをディレクトリに、次の内容の`project.json`ファイルを作成します。

```
{
  "tools": {
    "ReVIEWBlogger": {
      "version": "1.0.0-alpha5",
      "imports": ["dnxcore50"]
    }
  },
  "frameworks": {
    "netcoreapp1.0": {}
  }
}
```

### 必要な依存パッケージの取得

コマンド一発です。

```
$ dotnet restore
```


## 使い方

```
$ dotnet save-draft 1505_my-ever-best-blog-entry.re
```

これで、指定した`.re`ファイルがHTMLへ変換されてBloggerへ下書き保存されます。

TODO: authあたりのやり方を書く

画像の貼り付けはサポートしていないので、HTMLエディタへ貼り付けた後でビジュアル編集モードを使ってほどよく貼り付けてください(そもそもBloggerは画像アップロードAPIを持ちません)。

## ライセンス

MIT
