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
            new ImageMessage("https://line.ja-fleet.noobow.me/howtouse.jpg", "https://line.ja-fleet.noobow.me/howtouse.jpg")
        };

        public static readonly TextMessage ONLY_PHOTO = new TextMessage("JA-Fleetにデータが登録されていないため、写真のみ検索しました。");

        public static readonly TextMessage NOT_FOUND = new TextMessage("JA-Fleetにデータが登録されておらず、写真のみの検索でも見つかりませんでした。\n" +
                                                                "------------\n" +
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
