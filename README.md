# Prospect

Also known as "The Cycle: Frontier".

This repository is just something I work on when bored, do not expect much at this stage.

## Features

- [x] Connect with localhost instead of official servers
- [ ] Basic authentication
- [ ] Basic lobby functionality
- [ ] CloudScript
- [ ] ?

## Development setup

### 1. Download the game

If you are enrolled into the closed beta, you can download a fresh copy with [SteamRE/DepotDownloader](https://github.com/SteamRE/DepotDownloader).

```
.\DepotDownloader.exe -app 1600360 -depot 1600361 -manifest 8099885118311987420 -username <username>
```

This gives you the [exact copy](https://steamdb.info/depot/1600361/history/?changeid=M:8099885118311987420) that is used for this repository.

### 2. Modify your hosts file

Open `C:\Windows\System32\drivers\etc\hosts` with notepad as an Administrator. 

Add the following line to the bottom of the file and save it.
```
127.0.0.1 A22AB.localhost
```

### 3. Clone the repository

```
git clone https://github.com/AeonLucid/Prospect.git
```

### 4. Open the solution

Open the `src/Prospect.sln` in either Rider or Visual Studio.

### 5. Run the launcher

The game needs to be modified so that it connects our own servers. With the current implementation it will connect to `https://A22AB.localhost`. The `.localhost` part may not exceed `15` characters. 

Run the `Prospect.Launcher` project with the first argument being the path of the game.

```
./Prospect.Launcher.exe "E:\Depots\depots\1600361\7573497"
```

> The Cycle uses https://playfab.com/ for their multiplayer service. By default it connects to `titleId.playfabapi.com`, the `.playfabapi.com` part is hardcoded and the `titleId` can be set with `PF_TITLEID`. Known `titleId` values are `A22AB` (The Cycle Playtest) and `2EA46` (Fallback, hardcoded).