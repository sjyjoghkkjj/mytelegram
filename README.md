# MyTelegram

[![API Layer](https://img.shields.io/badge/API_Layer-200-blueviolet)](https://corefork.telegram.org/methods)
[![MTProto](https://img.shields.io/badge/MTProto_Protocol-2.0-green)](https://corefork.telegram.org/mtproto/)
[![Support Chat](https://img.shields.io/badge/Chat_with_us-on_Telegram-0088cc)](https://t.me/+S-aNBoRvCRpPyXrR)

MyTelegram is telegram server side api implementation written in c#,support private deployment

## Features

- API Layer: **`200`**
- [MTProto transports](https://corefork.telegram.org/mtproto/mtproto-transports): **`Abridged`**,**`Intermediate`**
- Private chat
- Group chat
- Supergroup chat
- Channel
- End-to-end-encryption chat(Pro version)
- Voice/video call(Pro version)
- Bot(Partial support, Pro version)
- 2FA(Pro version)
- Stickers(Pro version)
- Reactions(Pro version)
- ForumTopics(Pro version)
- Themes/Wallpapers/Auto-Delete Messages/Scheduled Messages/Telegram Business/Stories/Email Login/Email Sender/Push Server(Firebase) (Pro version)

## Build MyTelegram Server

1. Install [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
2. Run `build/build.ps1` or `build/build.sh`
3. Build output folder is `out/{version}`

## Build docker images

- ### linux/amd64 (build)
```
build-all-amd64.sh
```
- ### linux/arm64 (build)
```
build-all-arm64.sh
```
- ### linux/amd64 & linux/arm64 (build and push)
```
build-and-push-all-amd64-arm64.sh
```

## Run MyTelegram server

- ### Run MyTelegram server with docker

1. Download docker-compose configuration files
   ```
   https://raw.githubusercontent.com/loyldg/mytelegram/dev/docker/compose/docker-compose.yml

   https://raw.githubusercontent.com/loyldg/mytelegram/dev/docker/compose/.env
   ```
2. Replace `192.168.1.100` with your own server IP in `.env`
3. Run the following command in the directory where the docker-compose.yml file is located
   ```
      docker compose up
   ```
4. Default verification code is `22222`

## MyTelegram clients
[TDesktop for mytelegram](https://github.com/loyldg/mytelegram-tdesktop)

[Android client for mytelegram](https://github.com/loyldg/mytelegram-android)

[iOS client for mytelegram](https://github.com/loyldg/mytelegram-iOS)

[WebK for mytelegram](https://github.com/loyldg/mytelegram-webk)

[WebA for mytelegram](https://github.com/loyldg/mytelegram-weba)

1. Git clone the client source code
2. Replace `192.168.1.100` with your server IP

## Support MyTelegram

Love MyTelegram? Please give a star to this repository ⭐

## Feedback

Contact author: [https://t.me/mytelegram666](https://t.me/mytelegram666)  
Join telegram group: [https://t.me/+S-aNBoRvCRpPyXrR](https://t.me/+S-aNBoRvCRpPyXrR)
