# Re:VIEWのファイルをBloggerへ流し込めるようにするやつ

## 機能

https://github.com/muojp/review-blog-template/ のテンプレに従って記述した.reファイル(Re:VIEWドキュメント)をBloggerへ流し込むのに適した形式へと変換します。

footnoteに付与するid属性を記事横断でユニークにしたり、Blogger editorのURI変換によってリンクが壊れがちな箇所を回避したり、という機能を持ちます。

この先はBlogger APIを使ってドラフトの自動保存やカスタムURL設定あたりまでできるようにしたいところです。

インストール方法や使い方など、go全然慣れてない状態で書いたので流儀的におかしいかもしれません。指摘頂けると幸いです。

## 必要なもの(確認済み環境)

- Mac OS X 10.10
- go 1.4.2
- review-compile 1.5.0
 
## インストール方法

```
$ go get github.com/muojp/review-blogger
$ go install github.com/muojp/review-blogger
```

※あらかじめ`GOBIN`環境変数を設定しておいてください

## 使い方

```
$ review-blogger 1505_my-ever-best-blog-entry.re | pbcopy
```

1行目はタイトル、2行目以降が本文(BloggerのHTMLエディタへそのまま貼り付ける用)です。

画像の貼り付けなどはサポートしていないので、HTMLエディタへ貼り付けた後でビジュアル編集モードを使ってほどよく貼り付けてください(そもそもBloggerは画像アップロードAPIを持ちません)。

## ライセンス

MIT
