using Line.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jafleet.Line.Constants
{
    public static class ReplayMessage
    {
        public static readonly ISendMessage[] FOLLOW_MESSAGE = new ISendMessage[]
        {
            new TextMessage("フォローありがとうございます。\n" +
                                                "このアカウントのでは使用方法は以下の画像および説明をご確認ください。\n" +
                                                "・JA-Fleetサイトに登録されている飛行機は詳細情報と写真を検索できます\n" +
                                                "・JA-Fleetサイトに登録されていない場合は写真のみ検索できます\n" +
                                                "【検索例】\n" +
                                                "●JAレジ\n" +
                                                "801a,JA301J\n" +
                                                "（JAレジは、'JA'をつけなくてもOK）\n" +
                                                "●それ以外\n" +
                                                "80-1111,n501dn, A6-BLA\n" +
                                                "（省略不可）\n" +
                                                "※すべて大文字小文字区別せず"),
            new TextMessage("管理人にメッセージを送るには1行目に「メッセージ」と書いて、2行目以降にメッセージを入力してください。"),
            new ImageMessage("https://line.ja-fleet.noobow.me/howtouse.jpg", "https://line.ja-fleet.noobow.me/howtouse.jpg")
        };

        public static readonly TextMessage SEND_MESSAGE = new TextMessage("メッセージを受信しました。\n" +
                                                                            "ありがとうございましたm(_ _)m");

        public static readonly TextMessage NOT_FOUND = new TextMessage("見つかりませんでした。" +
                                                                       "----------\n" +
                                                                        "検索方法を確認するには「検索方法」と入力してください。" +
                                                                        "管理人にメッセージを送るには1行目に「メッセージ」と書いて、2行目以降にメッセージを入力してください。");

        public static readonly TextMessage HOWTO_SEARCH = new TextMessage("【検索対象】\n"+
                                                                "JA-Fleet登録データ：JAレジで運航中のもの、2018/09以降に抹消されてもの\n" +
                                                                "写真のみ検索：Jetphotosサイトに登録されているもの\n" +
                                                                "【検索例】\n" +
                                                                "●JAレジ\n" +
                                                                "801a,JA301J\n" +
                                                                "（JAレジは、'JA'をつけなくてもOK）\n" +
                                                                "●それ以外\n" +
                                                                "80-1111,n501dn, A6-BLA\n" +
                                                                "（省略不可）\n" +
                                                                "※すべて大文字小文字区別せず");
    }
}
