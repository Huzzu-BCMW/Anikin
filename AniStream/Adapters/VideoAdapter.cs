﻿using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AniStream.Utils;
using AniStream.Utils.Downloading;
using AniStream.Utils.Extensions;
using Juro.Models;
using Juro.Models.Anime;
using Juro.Models.Videos;
using Newtonsoft.Json;

namespace AniStream.Adapters;

public class VideoAdapter : RecyclerView.Adapter
{
    private readonly Activity _activity;
    private readonly AnimeInfo _anime;
    private readonly Episode _episode;
    private readonly VideoServer _videoServer;

    public List<VideoSource> Videos { get; set; }

    public VideoAdapter(
        Activity activity,
        AnimeInfo anime,
        Episode episode,
        VideoServer videoServer,
        List<VideoSource> videos)
    {
        _activity = activity;
        _anime = anime;
        _episode = episode;
        _videoServer = videoServer;
        Videos = videos;
    }

    class UrlViewHolder : RecyclerView.ViewHolder
    {
        public TextView urlQuality = default!;
        public TextView urlNote = default!;
        public TextView urlSize = default!;
        public ImageButton urlDownload = default!;

        public UrlViewHolder(View view) : base(view)
        {
            urlQuality = view.FindViewById<TextView>(Resource.Id.urlQuality)!;
            urlNote = view.FindViewById<TextView>(Resource.Id.urlNote)!;
            urlSize = view.FindViewById<TextView>(Resource.Id.urlSize)!;
            urlDownload = view.FindViewById<ImageButton>(Resource.Id.urlDownload)!;
        }
    }

    public override int ItemCount => Videos.Count;

    public override long GetItemId(int position) => position;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var urlViewHolder = holder as UrlViewHolder;

        var video = Videos[urlViewHolder!.BindingAdapterPosition];

        if (!string.IsNullOrEmpty(video.Resolution))
            urlViewHolder.urlQuality.Text = video.Resolution;
        else
            urlViewHolder.urlQuality.Text = "Default Quality";

        //if (video.Format == VideoType.Container)
        //{
        //    urlViewHolder.urlDownload.Visibility = ViewStates.Visible;
        //    urlViewHolder.urlDownload.Click += (s, e) =>
        //    {
        //        var downloader = new Downloader(Activity);
        //        downloader.Download($"{_anime.Title} - Ep-{_episode.Number}.mp4", video.VideoUrl, video.Headers);
        //    };
        //}
        //Test below
        urlViewHolder.urlDownload.Visibility = ViewStates.Visible;
        urlViewHolder.urlDownload.Click += async (s, e) =>
        {
            await new EpisodeDownloader().EnqueueAsync(_anime, _episode, video);
        };

        if (video.Size != null && video.Size > 0)
        {
            urlViewHolder.urlSize.Visibility = ViewStates.Visible;
            urlViewHolder.urlSize.Text = FileSizeToStringConverter.Instance.Convert(video.Size.Value);
        }

        urlViewHolder.ItemView.Click += (s, e) =>
        {
            if (_activity is VideoActivity videoActivity)
            {
                videoActivity.PlayVideo(video);
                return;
            }

            var intent = new Intent(_activity, typeof(VideoActivity));

            intent.PutExtra("anime", JsonConvert.SerializeObject(_anime));
            intent.PutExtra("episode", JsonConvert.SerializeObject(_episode));
            intent.PutExtra("video", JsonConvert.SerializeObject(video));
            intent.PutExtra("videoServer", JsonConvert.SerializeObject(_videoServer));
            intent.SetFlags(ActivityFlags.NewTask);

            _activity.ApplicationContext!.StartActivity(intent);
        };

        urlViewHolder.ItemView.LongClick += (s, e) =>
        {
            var url = video.VideoUrl.Replace(" ", "%20");
            var videoUri = Android.Net.Uri.Parse(url);

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(videoUri, "video/*");
            intent.SetFlags(ActivityFlags.NewTask);

            _activity.CopyToClipboard(url, false);
            _activity.ShowToast($"Copied \"{url}\"");

            var i = Intent.CreateChooser(intent, "Open Video in :")!;
            i.SetFlags(ActivityFlags.NewTask);

            _activity.ApplicationContext!.StartActivity(i);
        };
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var itemView = LayoutInflater.From(parent.Context)!
            .Inflate(Resource.Layout.item_url, parent, false)!;

        return new UrlViewHolder(itemView);
    }
}