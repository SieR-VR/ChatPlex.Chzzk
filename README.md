# ChatPlex.Chzzk

치지직 채팅 연동을 가능하게 해주는 [BeatSaberPlus](https://github.com/hardcpp/BeatSaberPlus) 애드온입니다.

## 기능

- 채팅 연동
- 이모지
- 채팅 뱃지
- 연결 실패 시 재연결

## 사용법

- (적용 전) 버전에 맞는 [BeatSaberPlus](https://github.com/hardcpp/BeatSaberPlus) 플러그인을 먼저 적용해주세요.
  - 작동에 필요한 필수 모드는 다음과 같습니다:
  - BeatSaberPlus_Chat
  - ChatPlexSDK_BS
- ChatPlex.Chzzk.dll 파일을 Plugins 폴더에 넣어 적용해주세요.
- 게임을 한 번 실행하고 나면 `(게임 디렉토리)/UserData` 폴더에 `ChatPlex.Chzzk.json` 파일이 생깁니다. 해당 파일의 `ChannelId` 필드를 본인의 채널 Id로 변경해야 합니다.
  - 치지직 스튜디오 대시보드의 Url 형식이 `https://studio.chzzk.naver.com/{ChannelId}/` 이므로 여기서 복사해오시면 됩니다.
- 파일 내용을 변경한 이후엔 꼭 게임을 재시작하여 플러그인을 다시 활성화해주세요.
- 방송이 켜져있다면 Id를 통해 자동으로 연결이 됩니다.

- BSML, BSIPA에 의존성이 있습니다. 거의 대부분의 환경에서는 신경쓰지 않으셔도 됩니다.

## 로드맵

- 브라우저 UI를 통해 치지직 계정 연동
- 채팅 보내기, 기타 관리 연동
- 1.40.8 지원

## 개발 가이드

클론 이후 루트 디렉토리에 `ChatPlex.Chzzk.csproj.user` 파일을 만들고,

```
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <BeatSaberDir>D:\BSManager\BSInstances\1.39.1</BeatSaberDir>
  </PropertyGroup>
</Project>
```

와 같은 식으로 비트세이버 디렉토리를 등록해줘야 빌드가 가능합니다.
