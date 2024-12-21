using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Imperial.ShowPopupOnJoin.Prototypes
{
    [Prototype("popupWindow")]
    public sealed class PopupWindowPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("title")]
        public string Title { get; set; } = default!;
        [DataField("content")]
        public string Content { get; set; } = default!;
        /// <summary>
        /// Оставьте пустым, что бы небыло изображения
        /// </summary>
        [DataField("qrcode")]
        public string QRCodePath { get; set; } = default!;
        [DataField("url")]
        public string Url { get; set; } = default!;
    }
}
