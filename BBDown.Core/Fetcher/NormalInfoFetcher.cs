using BBDown.Core.Entity;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Models.Media;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher
{
    public partial class NormalInfoFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
        {
            var videoInfo = await Config.Kernel.GetRequiredService<IPlayerService>().GetVideoPageDetailAsync(new Richasy.BiliKernel.Models.Media.MediaIdentifier(id, default, default));
            var info = videoInfo.Information;
            string title = info.Identifier.Title;
            string desc = info.GetExtensionIfNotNull<string>(VideoExtensionDataId.Description);
            string pic = info.Identifier.Cover.Uri.ToString();
            string ownerMid = info.Publisher.User.Id;
            string ownerName = info.Publisher.User.Name;
            long pubTime = info.PublishTime.Value.ToUnixTimeSeconds();
            bool bangumi = false;
            var bvid = info.BvId;
            var cid = info.GetExtensionIfNotNull<long>(VideoExtensionDataId.Cid);

            // 互动视频 1:是 0:否
            var isSteinGate = videoInfo.IsInteractiveVideo;

            // 分p信息
            List<Page> pagesInfo = new();
            var pages = videoInfo.Parts;
            foreach (var page in pages)
            {
                Page p = new(page.Index,
                    id,
                    page.Identifier.Id,
                    "", //epid
                    page.Identifier.Title,
                    page.Duration,
                    "",
                    pubTime, //分p视频没有发布时间
                    "",
                    "",
                    ownerName,
                    ownerMid
                );
                pagesInfo.Add(p);
            }

            if (isSteinGate) // 互动视频获取分P信息
            {
                var playerSoApi = $"https://api.bilibili.com/x/player.so?bvid={bvid}&id=cid:{cid}";
                var playerSoText = await GetWebSourceAsync(playerSoApi);
                var playerSoXml = new XmlDocument();
                playerSoXml.LoadXml($"<root>{playerSoText}</root>");
                
                var interactionNode = playerSoXml.SelectSingleNode("//interaction");

                if (interactionNode is { InnerText.Length: > 0 })
                {
                    var graphVersion = JsonDocument.Parse(interactionNode.InnerText).RootElement
                        .GetProperty("graph_version").GetInt64();
                    var edgeInfoApi = $"https://api.bilibili.com/x/stein/edgeinfo_v2?graph_version={graphVersion}&bvid={bvid}";
                    var edgeInfoJson = await GetWebSourceAsync(edgeInfoApi);
                    var edgeInfoData = JsonDocument.Parse(edgeInfoJson).RootElement.GetProperty("data");
                    var questions = edgeInfoData.GetProperty("edges").GetProperty("questions").EnumerateArray()
                        .ToList();
                    var index = 2; // 互动视频分P索引从2开始
                    foreach (var question in questions)
                    {
                        var choices = question.GetProperty("choices").EnumerateArray().ToList();
                        foreach (var page in choices)
                        {
                            Page p = new(index++,
                                id,
                                page.GetProperty("cid").ToString(),
                                "", //epid
                                page.GetProperty("option").ToString().Trim(),
                                0,
                                "",
                                pubTime, //分p视频没有发布时间
                                "",
                                "",
                                ownerName,
                                ownerMid
                            );
                            pagesInfo.Add(p);
                        }
                    }
                }
                else
                {
                    throw new Exception("互动视频获取分P信息失败");
                }
            }

            var vinfo = new VInfo
            {
                Title = title.Trim(),
                Desc = desc.Trim(),
                Pic = pic,
                PubTime = pubTime,
                PagesInfo = pagesInfo,
                IsBangumi = bangumi,
                IsSteinGate = isSteinGate
            };

            return vinfo;
        }

        [GeneratedRegex("ep(\\d+)")]
        private static partial Regex EpIdRegex();
    }
}
