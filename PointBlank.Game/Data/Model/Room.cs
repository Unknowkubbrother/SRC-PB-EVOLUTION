﻿using PointBlank.Core.Models.Enums;
using PointBlank.Game.Network.ServerPacket;
using PointBlank.Game.Data.Managers;
using PointBlank.Game.Data.Sync;
using PointBlank.Game.Data.Sync.Server;
using PointBlank.Game.Data.Utils;
using PointBlank.Game.Data.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PointBlank.Core.Network;
using PointBlank.Core.Models.Room;
using PointBlank.Core;
using PointBlank.Core.Managers.Events;
using PointBlank.Core.Managers;
using PointBlank.Core.Managers.Server;
using PointBlank.Core.Models.Account.Players;
using PointBlank.Core.Models.Account.Rank;
using PointBlank.Core.Xml;
using PointBlank.Core.Models.Account.Clan;
using PointBlank.Game.Data.Configs;
using PointBlank.Core.Models.Map;
using PointBlank.Battle.Data.Models;

namespace PointBlank.Game.Data.Model
{
    public class Room
    {
        public Slot[] _slots = new Slot[16];
        public bool Countdown = false;
        public int _channelType, rounds = 1, TRex = -1, blue_rounds, blue_dino, red_rounds, red_dino, Bar1, Bar2, _ping = 5, _redKills, _redDeaths, _redAssists, _blueKills, _blueDeaths, _blueAssists, spawnsCount, rule, killtime, _roomId, _channelId, _leader,_gp,_exp;
        public byte Limit, WatchRuleFlag, aiCount = 1, IngameAiLevel, aiLevel, aiType, stage;
        public short BalanceType;
        public readonly int[] TIMES = new int[11] 
        {
            3, 3, 3, 5, 7, 5, 10, 15, 20, 25, 30
        },  
            KILLS = new int[9] 
        {
            15, 30, 50, 60, 80, 100, 120, 140, 160
        },  
            ROUNDS = new int[6] 
        {
            1, 2, 3, 5, 7, 9
        }, 
            RED_TEAM = new int[8] 
        {
            0, 2, 4, 6, 8, 10, 12, 14
        },  
            BLUE_TEAM = new int[8] 
        {
            1, 3, 5, 7, 9, 11, 13, 15
        };
        public byte[] HitParts = new byte[35], DefaultParts = new byte[35] 
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
            30, 31, 32, 33, 34
        };
        public uint _timeRoom, StartDate, UniqueRoomId, Seed;
        public long StartTick;
        public string name, password, _mapName;
        public Core.Models.Room.VoteKick votekick;
        public MapIdEnum mapId;
        public RoomType RoomType;
        public RoomState RoomState;
        public GameRuleFlag RuleFlag = GameRuleFlag.ไม่มี;
        public RoomStageFlag Flag = RoomStageFlag.NONE;
        public RoomWeaponsFlag weaponsFlag = RoomWeaponsFlag.None;
        public bool C4_actived, swapRound, changingSlots, blockedClan, ShotgunMode, SniperMode;
        public BattleServer UdpServer;
        public DateTime BattleStart, LastPingSync = DateTime.Now;
        public TimerState bomb = new TimerState(), countdown = new TimerState(), round = new TimerState(), vote = new TimerState(), countdownstart = new TimerState();
        public SafeList<long> kickedPlayers = new SafeList<long>(), requestHost = new SafeList<long>();
        public bool GameRuleActive = false;
        public bool ShotgunActive = false;
        public bool BarrettActive = false;
        public bool MaskActive = false;
        public bool RPG7Active = false;
        public bool GameRuleWCActive = false;
        public List<GameRule> GameRules = new List<GameRule>();

        public Room(int roomId, Channel ch)
        {
            _roomId = roomId;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new Slot(i);
            }
            _channelId = ch._id;
            _channelType = ch._type;
            SetUniqueId();
            GameRules = GameRuleManager.getGameRules(GameConfig.ruleId);
        }

        public bool thisModeHaveCD()
        {
            RoomType stageType = RoomType;
            return (stageType == RoomType.Bomb ||
                    stageType == RoomType.Annihilation ||
                    stageType == RoomType.Ace ||
                    stageType == RoomType.Boss ||
                    stageType == RoomType.CrossCounter ||
                    stageType == RoomType.Convoy);
        }

        public bool thisModeHaveRounds()
        {
            RoomType stageType = RoomType;
            return (stageType == RoomType.Bomb ||
                    stageType == RoomType.Destroy ||
                    stageType == RoomType.Annihilation ||
                    stageType == RoomType.Ace ||
                    stageType == RoomType.Defense ||
                    stageType == RoomType.Convoy);
        }

        public int getFlag()
        {
            int Result = 0;
            if (Flag.HasFlag(RoomStageFlag.RANDOM_MAP))
            {
                Result += 2;
            }
            if (Flag.HasFlag(RoomStageFlag.PASSWORD) || password.Length > 0)
            {
                Result += 4;
            }
            if (BalanceType == 1)
            {
                Result += 32;
            }
            if (Limit > 0 && RoomState > RoomState.Ready)
            {
                Result += 128;
            }
            Flag = (RoomStageFlag)Result;
            return Result;
        }

        public void LoadHitParts()
        {
            Random rnd = new Random();
            int next = rnd.Next(34);
            byte[] MyRandomArray = DefaultParts.OrderBy(x => x <= next).ToArray();

            Logger.warning("Idx: " + next + "/ Hits: " + BitConverter.ToString(MyRandomArray));
            HitParts = MyRandomArray;

            byte[] newarray = new byte[35];
            for (int i = 0; i < 35; i++)
            {
                byte valor = HitParts[i];
                newarray[((i + 8) % 35)] = valor;
            }
            Logger.warning("Array: " + BitConverter.ToString(newarray));
        }

        private void SetUniqueId()
        {
            UniqueRoomId = (uint)((GameConfig.serverId & 0xff) << 20 | (_channelId & 0xff) << 12 | _roomId & 0xfff);
        }

        public void SetSeed()
        {
            Seed = (uint)(((int)mapId & 0xff) << 20 | (rule & 0xff) << 12 | (int)RoomType & 0xfff);
        }

        public void SetBotLevel()
        {
            if (!isBotMode())
            {
                return;
            }
            IngameAiLevel = aiLevel;
            for (int i = 0; i < 16; i++)
            {
                _slots[i].aiLevel = IngameAiLevel;
            }
        }

        public bool isBotMode()
        {
            return stage == 2 || stage == 4 || stage == 6;
        }

        private void SetSpecialStage()
        {
            if (RoomType == RoomType.Defense)
            {
                if (mapId == MapIdEnum.BlackPanther)
                {
                    Bar1 = 6000;
                    Bar2 = 9000;
                }
            }
            else if (RoomType == RoomType.Destroy)
            {
                if (mapId == MapIdEnum.Hospital)
                {
                    Bar1 = 12000;
                    Bar2 = 12000;
                }
                else if (mapId == MapIdEnum.BreakDown)
                {
                    Bar1 = 6000;
                    Bar2 = 6000;
                }
            }
        }

        public int getInBattleTime()
        {
            int seconds = 0;
            if (BattleStart != new DateTime() && (RoomState == RoomState.Battle || RoomState == RoomState.PreBattle))
            {
                DateTime now = DateTime.Now;
                seconds = (int)(now - BattleStart).TotalSeconds;
                if (seconds < 0)
                {
                    seconds = 0;
                }
            }
            return seconds;
        }

        public int getInBattleTimeLeft()
        {
            int remaining = getInBattleTime();
            return ((getTimeByMask() * 60) - remaining);
        }

        public Channel getChannel()
        {
            return ChannelsXml.getChannel(_channelId);
        }

        public bool getChannel(out Channel ch)
        {
            ch = ChannelsXml.getChannel(_channelId);
            return ch != null;
        }

        public bool getSlot(int slotIdx, out Slot slot)
        {
            slot = null;
            lock (_slots)
            {
                if (slotIdx >= 0 && slotIdx <= 15)
                {
                    slot = _slots[slotIdx];
                }
                return slot != null;
            }
        }

        public Slot getSlot(int slotIdx)
        {
            lock (_slots)
            {
                if (slotIdx >= 0 && slotIdx <= 15)
                {
                    return _slots[slotIdx];
                }
                return null;
            }
        }

        public void StartCounter(int type, Account player, Slot slot)
        {
            int dueTime = 0;
            EventErrorEnum error = 0;
            if (type == 0)
            {
                error = EventErrorEnum.BATTLE_FIRST_MAINLOAD;
                dueTime = 90000;
            }
            else if (type == 1)
            {
                error = EventErrorEnum.BATTLE_FIRST_HOLE;
                dueTime = 30000;
            }
            else
            {
                return;
            }
            slot.timing.Start(dueTime, (callbackState) =>
            {
                BaseCounter(error, player, slot);
                lock (callbackState)
                {
                    if (slot != null)
                    {
                        slot.StopTiming();
                    }
                }
            });
        }

        private void BaseCounter(EventErrorEnum error, Account player, Slot slot)
        {
            player.SendPacket(new PROTOCOL_SERVER_MESSAGE_KICK_BATTLE_PLAYER_ACK(error));
            player.SendPacket(new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, 0));
            slot.state = SlotState.NORMAL;
            AllUtils.BattleEndPlayersCount(this, isBotMode());
            updateSlotsInfo();
        }

        public void StartBomb()
        {
            try
            {
                bomb.Start(42000, (callbackState) =>
                {
                    if (this != null && C4_actived)
                    {
                        red_rounds++;
                        C4_actived = false;
                        AllUtils.BattleEndRound(this, 0, RoundEndType.BombFire);
                    }
                    lock (callbackState)
                    {
                        bomb.Timer = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.warning("StartBomb: " + ex.ToString());
            }
        }

        public void StartVote()
        {
            try
            {
                if (votekick == null)
                {
                    return;
                }

                vote.Start(20000, (callbackState) =>
                {
                    AllUtils.votekickResult(this);
                    lock (callbackState)
                    {
                        vote.Timer = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.warning("StartVote: " + ex.ToString());
                if (vote.Timer != null)
                {
                    vote.Timer = null;
                }
                votekick = null;
            }
        }

        public void RoundRestart()
        {
            try
            {
                StopBomb();
                foreach (Slot s in _slots)
                {
                    if (s._playerId > 0 && (int)s.state == 15)
                    {
                        if (!s._deathState.HasFlag(DeadEnum.UseChat))
                        {
                            s._deathState |= DeadEnum.UseChat;
                        }
                        if (s.espectador)
                        {
                            s.espectador = false;
                        }
                        if (s.killsOnLife >= 3 && RoomType == RoomType.Annihilation)
                        {
                            s.objects++;
                        }
                        if (s.killsOnLife >= 3 && RoomType == RoomType.Ace)
                        {
                            s.objects++;
                        }
                        s.killsOnLife = 0;
                        s.lastKillState = 0;
                        s.repeatLastState = false;
                        s.damageBar1 = 0;
                        s.damageBar2 = 0;
                    }
                }
                round.Start(8000, (callbackState) =>
                {
                    foreach (Slot s in _slots)
                    {
                        if (s._playerId > 0)
                        {
                            if (!s._deathState.HasFlag(DeadEnum.UseChat))
                            {
                                s._deathState |= DeadEnum.UseChat;
                            }
                            if (s.espectador)
                            {
                                s.espectador = false;
                            }
                        }
                    }
                    StopBomb();
                    DateTime now = DateTime.Now;
                    if (RoomState == RoomState.Battle)
                    {
                        BattleStart = RoomType == RoomType.Boss || RoomType == RoomType.CrossCounter ? now.AddSeconds(5) : now;
                    }
                    using (PROTOCOL_BATTLE_MISSION_ROUND_PRE_START_ACK packet = new PROTOCOL_BATTLE_MISSION_ROUND_PRE_START_ACK(this))
                    {
                        using (PROTOCOL_BATTLE_MISSION_ROUND_START_ACK packet2 = new PROTOCOL_BATTLE_MISSION_ROUND_START_ACK(this))
                        {
                            SendPacketToPlayers(packet, packet2, SlotState.BATTLE, 0);
                        }
                    }
                    StopBomb();
                    swapRound = false;
                    //AllUtils.LogRoomRoundRestart(this);
                    lock (callbackState)
                    {
                        round.Timer = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.warning("[Room.RoundRestart] " + ex.ToString());
            }
        }

        public void StopBomb()
        {
            if (!C4_actived)
            {
                return;
            }
            C4_actived = false;
            if (bomb != null)
            {
                bomb.Timer = null;
            }
        }

        public void StartBattle(bool updateInfo)
        {
            Monitor.Enter(_slots);
            RoomState = RoomState.Loading;
            requestHost.Clear();
            UdpServer = BattleServerXml.GetRandomServer();
            StartTick = DateTime.Now.Ticks;
            StartDate = uint.Parse(DateTime.Now.ToString("yyMMddHHmm"));
            SetBotLevel();
            AllUtils.CheckClanMatchRestrict(this);

            using (PROTOCOL_BATTLE_START_GAME_ACK packet = new PROTOCOL_BATTLE_START_GAME_ACK(this))
            {
                byte[] data = packet.GetCompleteBytes("Room.StartBattle");
                foreach(Account pR in getAllPlayers(SlotState.READY, 0))
                {
                    Slot slot = getSlot(pR._slotId);
                    if (slot != null)
                    {
                        slot.withHost = true;
                        slot.state = SlotState.LOAD;
                        slot.SetMissionsClone(pR._mission);
                        pR.SendCompletePacket(data);
                    }
                }
            }
            if (updateInfo)
            {
                updateSlotsInfo();
            }
            updateRoomInfo();
            Monitor.Exit(_slots);
        }

        public void StartCountDown()
        {
            using (PROTOCOL_BATTLE_START_COUNTDOWN_ACK packet = new PROTOCOL_BATTLE_START_COUNTDOWN_ACK(CountDownEnum.Start))
            {
                SendPacketToPlayers(packet);
            }
            countdown.Start(5250, (callbackState) =>
            {
                try
                {
                    if (_slots[_leader].state == SlotState.READY && RoomState == RoomState.CountDown)
                    {
                        StartBattle(true);
                    }
                    //else
                    //{
                    //    Logger.warning("[Room.StartCountDown] Sala: " + _roomId + "; Canal: " + _channelId + "; state: " + _state + "; leader: " + _slots[_leader].state);
                    //    Logger.warning("[Data: " + DateTime.Now.ToString("dd/MM/yy HH:mm:ss"));
                    //}
                }
                catch (Exception ex)
                {
                    Logger.warning("[Room.StartCountDown] " + ex.ToString());
                }
                lock (callbackState)
                {
                    countdown.Timer = null;
                }
            });
        }

        public void StopCountDown(CountDownEnum motive, bool refreshRoom = true)
        {
            RoomState = RoomState.Ready;
            if (refreshRoom)
            {
                updateRoomInfo();
            }
            countdown.Timer = null;
            using (PROTOCOL_BATTLE_START_COUNTDOWN_ACK packet = new PROTOCOL_BATTLE_START_COUNTDOWN_ACK(motive))
            {
                SendPacketToPlayers(packet);
            }
        }

        public void StopCountDown(int slotId)
        {
            if (RoomState != RoomState.CountDown)
            {
                return;
            }
            if (slotId == _leader)
            {
                StopCountDown(CountDownEnum.StopByHost);
            }
            else if (getPlayingPlayers(_leader % 2 == 0 ? 1 : 0, SlotState.READY, 0) == 0)
            {
                changeSlotState(_leader, SlotState.NORMAL, false);
                StopCountDown(CountDownEnum.StopByPlayer);
            }
        }

        public void CalculateResult()
        {
            lock (_slots)
            {
                BaseResultGame(AllUtils.GetWinnerTeam(this), isBotMode());
            }
        }

        public void CalculateResult(TeamResultType resultType)
        {
            lock (_slots)
            {
                BaseResultGame(resultType, isBotMode());
            }
        }

        public void CalculateResult(TeamResultType resultType, bool isBotMode)
        {
            lock (_slots)
            {
                BaseResultGame(resultType, isBotMode);
            }
        }

        private void BaseResultGame(TeamResultType winnerTeam, bool isBotMode)
        {
            ServerConfig config = GameManager.Config;
            EventUpModel runningEvent = EventRankUpSyncer.getRunningEvent();
            EventMapModel runningEvent2 = EventMapSyncer.getRunningEvent();
            bool flag = EventMapSyncer.EventIsValid(runningEvent2, (int)mapId, (int)RoomType);
            PlayTimeModel runningEvent3 = EventPlayTimeSyncer.getRunningEvent();
            DateTime now = DateTime.Now;
            if (config == null)
            {
                Logger.error("Server Config Null. RoomResult canceled.");
                return;
            }
            Slot[] slots = _slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                Account account;
                if (!slot.check && slot.state == SlotState.BATTLE && getPlayerBySlot(slot, out account))
                {
                    DBQuery dBQuery = new DBQuery();
                    DBQuery dBQuery2 = new DBQuery();
                    slot.check = true;
                    double num = slot.inBattleTime(now);
                    int gp = account._gp;
                    int exp = account._exp;
                    int money = account._money;
                    if (!isBotMode)
                    {
                        if (config.missions)
                        {
                            AllUtils.endMatchMission(this, account, slot, winnerTeam);
                            if (slot.MissionsCompleted)
                            {
                                account._mission = slot.Missions;
                                MissionManager.getInstance().updateCurrentMissionList(account.player_id, account._mission);
                            }
                            AllUtils.GenerateMissionAwards(account, dBQuery);
                        }
                        int num2 = (slot.allKills == 0 && slot.allDeaths == 0) ? ((int)(num / 3.0)) : ((int)num);
                        if (RoomType == RoomType.Bomb || RoomType == RoomType.Annihilation || RoomType == RoomType.Ace)
                        {
                            slot.exp = (int)(slot.Score + num2 / 2.5 + slot.allDeaths * 2.2 + (slot.objects * 20));
                            slot.gp = (int)(slot.Score + num2 / 3.0 + slot.allDeaths * 2.2 + (slot.objects * 20));
                            slot.money = (int)((slot.Score / 2) + num2 / 6.5 + slot.allDeaths * 1.5 + (slot.objects * 10));
                        }
                        else
                        {
                            slot.exp = (int)(slot.Score + num2 / 2.5 + slot.allDeaths * 1.8 + (slot.objects * 20));
                            slot.gp = (int)(slot.Score + num2 / 3.0 + slot.allDeaths * 1.8 + (slot.objects * 20));
                            slot.money = (int)(slot.Score / 1.5 + num2 / 4.5 + slot.allDeaths * 1.1 + (slot.objects * 20));
                        }
                        bool flag2 = slot._team == (int)winnerTeam;
                        if (rule != 80 && rule != 32)
                        {
                            account._statistic.headshots_count += slot.headshots;
                            account._statistic.kills_count += slot.allKills;
                            account._statistic.totalkills_count += slot.allKills;
                            account._statistic.deaths_count += slot.allDeaths;
                            account._statistic.assist += slot.allAssists;
                            AddKDInfosToQuery(slot, account._statistic, dBQuery);
                            AllUtils.updateMatchCount(flag2, account, (int)winnerTeam, dBQuery);
                            if (account.Daily != null)
                            {
                                account.Daily.Kills += slot.allKills;
                                account.Daily.Deaths += slot.allDeaths;
                                account.Daily.Headshots += slot.headshots;
                                AddDailyToQuery(slot, account.Daily, dBQuery2);
                                AllUtils.UpdateDailyRecord(flag2, account, (int)winnerTeam, dBQuery2);
                            }
                        }
                        if (flag2)
                        {
                            slot.gp += AllUtils.percentage(slot.gp, 15);
                            slot.exp += AllUtils.percentage(slot.exp, 20);
                        }
                        if (slot.earnedXP > 0)
                        {
                            slot.exp += slot.earnedXP * 5;
                        }
                    }
                    else
                    {
                        slot.gp += 300;
                        slot.exp += 300;
                    }
                    slot.exp = ((slot.exp > GameConfig.maxBattleXP) ? GameConfig.maxBattleXP : slot.exp);
                    slot.gp = ((slot.gp > GameConfig.maxBattleGP) ? GameConfig.maxBattleGP : slot.gp);
                    slot.money = ((slot.money > GameConfig.maxBattleMY) ? GameConfig.maxBattleMY : slot.money);
                    if (slot.exp < 0 || slot.gp < 0 || slot.money < 0)
                    {
                        slot.exp = 2;
                        slot.gp = 2;
                        slot.money = 2;
                    }
                    int num3 = 0;
                    int num4 = 0;
                    int num5 = 0;
                    int num6 = 0;
                    int num7 = 0;
                    int num8 = 0;
                    if (runningEvent != null | flag)
                    {
                        if (runningEvent != null)
                        {
                            num7 += runningEvent._percentXp;
                            num8 += runningEvent._percentGp;
                        }
                        if (flag)
                        {
                            num7 += runningEvent2._percentXp;
                            num8 += runningEvent2._percentGp;
                        }
                        if (!slot.bonusFlags.HasFlag(ResultIcon.Event))
                        {
                            slot.bonusFlags |= ResultIcon.Event;
                        }
                        slot.BonusEventExp += AllUtils.percentage(num7, 100);
                        slot.BonusEventPoint += AllUtils.percentage(num8, 100);
                    }
                    PlayerBonus bonus = account._bonus;
                    if (bonus != null && bonus.bonuses > 0)
                    {
                        if ((bonus.bonuses & 8) == 8)
                        {
                            num3 += 100;
                        }
                        if ((bonus.bonuses & 128) == 128)
                        {
                            num4 += 100;
                        }
                        if ((bonus.bonuses & 4) == 4)
                        {
                            num3 += 50;
                        }
                        if ((bonus.bonuses & 64) == 64)
                        {
                            num4 += 50;
                        }
                        if ((bonus.bonuses & 2) == 2)
                        {
                            num3 += 30;
                        }
                        if ((bonus.bonuses & 32) == 32)
                        {
                            num4 += 30;
                        }
                        if ((bonus.bonuses & 1) == 1)
                        {
                            num3 += 10;
                        }
                        if ((bonus.bonuses & 16) == 16)
                        {
                            num4 += 10;
                        }
                        if (!slot.bonusFlags.HasFlag(ResultIcon.Item))
                        {
                            slot.bonusFlags |= ResultIcon.Item;
                        }
                        slot.BonusItemExp += num3;
                        slot.BonusItemPoint += num4;
                    }

                    if (account.tourneyLevel == 1) // ตามเลขเลเวลตรงนี้
                    {
                        _gp += 100; // เงิน
                        _exp += 100; // exp อยากให้เวลไหน บวก exp หรือ เงิน กี่ % ตอนจบเกมก็ ปรับได้เลย 
                                       //      pctgMY += 100; // Cash
                    }
                    else if (account.tourneyLevel == 2)
                    {
                        _gp += 200;
                        _exp += 200;
                        //      pctgMY += 200;
                    }
                    else if (account.tourneyLevel == 3)
                    {
                        _gp += 300;
                        _exp += 300;
                        //     pctgMY += 300;
                    }
                    else if (account.tourneyLevel == 4)
                    {
                        _gp += 400;
                        _exp += 400;
                        //    pctgMY += 400;
                    }
                    else if (account.tourneyLevel == 5)
                    {
                        _gp += 500;
                        _exp += 500;
                        //    pctgMY += 500;
                    }
                    else if (account.tourneyLevel == 6)
                    {
                        _gp += 600;
                        _exp += 600;
                        //    pctgMY += 600;
                    }
                    else if (account.tourneyLevel == 7)
                    {
                        _gp += 700;
                        _exp += 700;
                        //  pctgMY += 700;
                    }
                    else if (account.tourneyLevel == 8)
                    {
                        _gp += 800;
                        _exp += 800;
                        //    pctgMY += 800;
                    }
                    else if (account.tourneyLevel == 9)
                    {
                        _gp += 900;
                        _exp += 900;
                        //   pctgMY += 900;
                    }
                    else if (account.tourneyLevel == 10)
                    {
                        _gp += 1000;
                        _exp += 1000;
                        //    pctgMY += 1000;
                    }
                    else if (account.tourneyLevel == 11)
                    {
                        _gp += 1500;
                        _exp += 1500;
                        //  pctgMY += 1500;
                    }
                    else if (account.tourneyLevel == 12)
                    {
                        _gp += 2000;
                        _exp += 2000;
                        //     pctgMY += 2000;
                    }
                    else if (account.tourneyLevel == 13)
                    {
                        _gp += 2500;
                        _exp += 2500;
                        //  pctgMY += 2500;
                    }
                    else if (account.tourneyLevel == 14)
                    {
                        _gp += 3000;
                        _exp += 3000;
                        //     pctgMY += 3000;
                    }
                    else if (account.tourneyLevel == 15)
                    {
                        _gp += 3500;
                        _exp += 3500;
                        //   pctgMY += 3500;
                    }
                    else if (account.tourneyLevel == 16)
                    {
                        _gp += 4000;
                        _exp += 4000;
                        //    pctgMY += 4000;
                    }
                    else if (account.tourneyLevel == 17)
                    {
                        _gp += 4500;
                        _exp += 4500;
                        //     pctgMY += 4500;
                    }
                    else if (account.tourneyLevel == 18)
                    {
                        _gp += 5000;
                        _exp += 5000;
                        //    pctgMY += 5000;
                    }
                    else if (account.tourneyLevel == 19)
                    {
                        _gp += 5500;
                        _exp += 5500;
                        //   pctgMY += 5500;
                    }
                    else if (account.tourneyLevel == 20)
                    {
                        _gp += 6000;
                        _exp += 6000;
                        //   pctgMY += 6000;
                    }

                    if (account.pc_cafe == 2 || account.pc_cafe == 1)
                    {
                        num5 += ((account.pc_cafe == 2) ? 120 : GameConfig.ICafeExp);
                        num6 += ((account.pc_cafe == 2) ? 100 : GameConfig.ICafePoint);
                        if (account.pc_cafe == 1 && !slot.bonusFlags.HasFlag(ResultIcon.Pc))
                        {
                            slot.bonusFlags |= ResultIcon.Pc;
                        }
                        else if (account.pc_cafe == 2 && !slot.bonusFlags.HasFlag(ResultIcon.PcPlus))
                        {
                            slot.bonusFlags |= ResultIcon.PcPlus;
                        }
                        slot.BonusCafePoint += num6;
                        slot.BonusCafeExp += num5;
                    }
                    if (isBotMode)
                    {
                        if (slot.BonusItemExp > 300)
                        {
                            slot.BonusItemExp = 300;
                        }
                        if (slot.BonusItemPoint > 300)
                        {
                            slot.BonusItemPoint = 300;
                        }
                        if (slot.BonusCafeExp > 300)
                        {
                            slot.BonusCafeExp = 300;
                        }
                        if (slot.BonusCafePoint > 300)
                        {
                            slot.BonusCafePoint = 300;
                        }
                        if (slot.BonusEventExp > 300)
                        {
                            slot.BonusEventExp = 300;
                        }
                        if (slot.BonusEventPoint > 300)
                        {
                            slot.BonusEventPoint = 300;
                        }
                    }
                    account._gp += slot.gp + slot.BonusItemPoint + slot.BonusEventPoint + slot.BonusCafePoint;
                    account._exp += slot.exp + slot.BonusItemExp + slot.BonusEventExp + slot.BonusCafeExp;
                    if (account.Daily != null)
                    {
                        account.Daily.Point += slot.gp + slot.BonusItemPoint + slot.BonusEventPoint + slot.BonusCafePoint;
                        account.Daily.Exp += slot.exp + slot.BonusItemExp + slot.BonusEventExp + slot.BonusCafeExp;
                        dBQuery2.AddQuery("point", account.Daily.Point);
                        dBQuery2.AddQuery("exp", account.Daily.Exp);
                    }
                    if (GameConfig.winCashPerBattle)
                    {
                        account._money += slot.money;
                    }
                    RankModel rank = RankXml.getRank(account._rank);
                    if (rank != null && account._exp >= rank._onNextLevel + rank._onAllExp && account._rank <= 50)
                    {
                        List<ItemsModel> awards = RankXml.getAwards(account._rank);
                        if (awards.Count > 0)
                        {
                            for (int j = 0; j < awards.Count; j++)
                            {
                                ItemsModel itemsModel = awards[j];
                                if (itemsModel._id != 0)
                                {
                                    account.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, account, itemsModel));
                                }
                            }
                        }
                        account._gp += rank._onGPUp;
                        account.LastRankUpDate = uint.Parse(now.ToString("yyMMddHHmm"));
                        Account arg_A46_0 = account;
                        Account expr_A27 = account;
                        int rank2 = expr_A27._rank + 1;
                        expr_A27._rank = rank2;
                        arg_A46_0.SendPacket(new PROTOCOL_BASE_RANK_UP_ACK(rank2, rank._onNextLevel));
                        dBQuery.AddQuery("last_rankup_date", (long)((ulong)account.LastRankUpDate));
                        dBQuery.AddQuery("rank", account._rank);
                    }
                    if (runningEvent3 != null)
                    {
                        AllUtils.PlayTimeEvent((long)num, account, runningEvent3, isBotMode);
                    }
                    AllUtils.DiscountPlayerItems(slot, account);
                    if (gp != account._gp)
                    {
                        dBQuery.AddQuery("gp", account._gp);
                    }
                    if (exp != account._exp)
                    {
                        dBQuery.AddQuery("exp", account._exp);
                    }
                    if (money != account._money)
                    {
                        dBQuery.AddQuery("money", account._money);
                    }
                    ComDiv.updateDB("accounts", "player_id", account.player_id, dBQuery.GetTables(), dBQuery.GetValues());
                    ComDiv.updateDB("player_dailyrecord", "player_id", account.player_id, dBQuery2.GetTables(), dBQuery2.GetValues());
                    if (GameConfig.winCashPerBattle && GameConfig.showCashReceiveWarn)
                    {
                        account.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Translation.GetLabel("CashReceived", new object[]
                        {
                            slot.money
                        })));
                    }
                }
            }
            updateSlotsInfo();
            CalculateClanMatchResult((int)winnerTeam);
        }

        private void AddKDInfosToQuery(Slot slot, PlayerStats stats, DBQuery query)
        {
            if (slot.allKills > 0)
            {
                query.AddQuery("kills_count", stats.kills_count);
                query.AddQuery("totalkills_count", stats.totalkills_count);
            }
            if (slot.allAssists > 0)
            {
                query.AddQuery("assist", stats.assist);
            }
            if (slot.allDeaths > 0)
            {
                query.AddQuery("deaths_count", stats.deaths_count);
            }
            if (slot.headshots > 0)
            {
                query.AddQuery("headshots_count", stats.headshots_count);
            }
        }

        private void AddDailyToQuery(Slot slot, PlayerDailyRecord Daily, DBQuery query)
        {
            if (Daily.Kills > 0)
            {
                query.AddQuery("kills", Daily.Kills);
            }
            if (Daily.Deaths > 0)
            {
                query.AddQuery("deaths", Daily.Deaths);
            }
            if (Daily.Headshots > 0)
            {
                query.AddQuery("headshots", Daily.Headshots);
            }
        }

        private void CalculateClanMatchResult(int winnerTeam)
        {
            if (_channelType != 4 || blockedClan)
            {
                return;
            }
            SortedList<int, Clan> list = new SortedList<int, Clan>();
            foreach (Slot slot in _slots)
            {
                Account p;
                if (slot.state == SlotState.BATTLE && getPlayerBySlot(slot, out p))
                {
                    Clan clan = ClanManager.getClan(p.clanId);
                    if (clan._id == 0)
                    {
                        continue;
                    }
                    bool WonTheMatch = (slot._team == winnerTeam);
                    clan._exp += slot.exp;
                    clan.BestPlayers.SetBestExp(slot);
                    clan.BestPlayers.SetBestKills(slot);
                    clan.BestPlayers.SetBestHeadshot(slot);
                    clan.BestPlayers.SetBestWins(p._statistic, slot, WonTheMatch);
                    clan.BestPlayers.SetBestParticipation(p._statistic, slot);
                    if (!list.ContainsKey(p.clanId))
                    {
                        list.Add(p.clanId, clan);
                        if (winnerTeam == 2)
                        {
                            goto UpdateClanFights;
                        }
                        CalculateSpecialCM(clan, winnerTeam, slot._team);
                        if (WonTheMatch)
                        {
                            clan.vitorias++;
                        }
                        else
                        {
                            clan.derrotas++;
                        }
                    UpdateClanFights:
                        PlayerManager.updateClanBattles(clan._id, ++clan.partidas, clan.vitorias, clan.derrotas);
                    }
                }
            }
            foreach (Clan clan in list.Values)
            {
                PlayerManager.updateClanExp(clan._id, clan._exp);
                PlayerManager.updateClanPoints(clan._id, clan._pontos);
                PlayerManager.updateBestPlayers(clan);
                RankModel rankModel = ClanRankXml.getRank(clan._rank);
                if (rankModel != null && clan._exp >= rankModel._onNextLevel + rankModel._onAllExp)
                {
                    PlayerManager.updateClanRank(clan._id, ++clan._rank);
                }
            }
        }

        private void CalculateSpecialCM(Clan clan, int winnerTeam, int teamIdx)
        {
            if (winnerTeam == 2)
            {
                return;
            }
            if (winnerTeam == teamIdx)
            {
                float morePoints = 0;
                if (RoomType == RoomType.DeathMatch)
                {
                    morePoints = (teamIdx == 0 ? _redKills : _blueKills) / 20;
                }
                else
                {
                    morePoints = (teamIdx == 0 ? red_rounds : blue_rounds);
                }
                float POINTS = (25 + morePoints);
                //Logger.warning("Clan: " + clan._id + " Earned Points: " + POINTS);
                clan._pontos += POINTS;
                //Logger.warning("Clan: " + clan._id + " Final Points: " + clan._pontos);
            }
            else
            {
                if (clan._pontos == 0)
                {
                    //Logger.warning("Clã não perdeu Pontos devido a baixa pontuação.");
                    return;
                }
                float morePoints = 0;
                if (RoomType == RoomType.DeathMatch)
                {
                    morePoints = (teamIdx == 0 ? _redKills : _blueKills) / 20;
                }
                else
                {
                    morePoints = (teamIdx == 0 ? red_rounds : blue_rounds);
                }
                float POINTS = (40 - morePoints);
                //Logger.warning("Clan: " + clan._id + " Losed Points: " + POINTS);
                clan._pontos -= POINTS;
                //Logger.warning("Clan: " + clan._id + " Final Points: " + clan._pontos);
            }
        }

        public bool isStartingMatch()
        {
            return RoomState > RoomState.Ready;
        }

        public bool isPreparing()
        {
            return RoomState >= RoomState.Loading;
        }

        public void updateRoomInfo()
        {
            SetSeed();
            using (PROTOCOL_ROOM_CHANGE_ROOMINFO_ACK packet = new PROTOCOL_ROOM_CHANGE_ROOMINFO_ACK(this))
            {
                SendPacketToPlayers(packet);
            }
        }

        public void initSlotCount(int count, bool Change = false)
        {
            MapMatch Match = Core.Models.Map.MapModel.Matchs.Find(x => x.Id == (int)mapId && Core.Models.Map.MapModel.getRule(x.Mode).Rule == rule);
            if (Match != null)
            {
                count = Match.Limit;
            }
            /*for (int i = 0; i < MapModel.Matchs.Count; i++)
            {
                MapMatch Match = MapModel.Matchs[i];
                if (Match.Id == (int)mapId && MapModel.getRule(Match.Mode).Rule == rule)
                {
                    count = Match.Limit;
                }
            }*/
            if (RoomType == RoomType.Tutorial)
            {
                count = 1;
            }
            if (isBotMode())
            {
                count = 8;
            }
            /*if (stage == 1)
            {
                count = 8;
            }*/
            if (count <= 0)
            {
                count = 1;
            }
            for (int index = 0; index < _slots.Length; ++index)
            {
                if (index >= count)
                {
                    _slots[index].state = SlotState.CLOSE;
                }
            }
            if (Change)
            {
                updateSlotsInfo();
            }
        }

        public int getSlotCount()
        {
            lock (_slots)
            {
                int count = 0;
                for (int index = 0; index < _slots.Length; ++index)
                {
                    if (_slots[index].state != SlotState.CLOSE)
                    {
                        ++count;
                    }
                }
                return count;
            }
        }

        public void SwitchNewSlot(List<SlotChange> slots, Account p, Slot old, int teamIdx, bool Mode)
        {
            if (Mode)
            {
                Slot slot = _slots[teamIdx];
                if (slot._playerId == 0 && (int)slot.state == 0)
                {
                    slot.state = SlotState.NORMAL;
                    slot._playerId = p.player_id;
                    slot._equip = p._equip;

                    old.state = SlotState.EMPTY;
                    old._playerId = 0;
                    old._equip = null;

                    if (p._slotId == _leader)
                    {
                        _leader = teamIdx;
                    }
                    p._slotId = teamIdx;
                    slots.Add(new SlotChange { oldSlot = old, newSlot = slot });
                }
            }
            else
            {
                for (int i = 0; i < GetTeamArray(teamIdx).Length; i++)
                {
                    int index = GetTeamArray(teamIdx)[i];
                    Slot slot = _slots[index];
                    if (slot._playerId == 0 && (int)slot.state == 0)
                    {
                        slot.state = SlotState.NORMAL;
                        slot._playerId = p.player_id;
                        slot._equip = p._equip;

                        old.state = SlotState.EMPTY;
                        old._playerId = 0;
                        old._equip = null;

                        if (p._slotId == _leader)
                        {
                            _leader = index;
                        }
                        //Logger.LogProblems("[Room.SwitchNewSlot] PlayerId '" + p.player_id + "' '" + p.player_name + "'; OldSlot: " + p._slotId + "; NewSlot: " + index, "ErrorC");
                        p._slotId = index;
                        slots.Add(new SlotChange { oldSlot = old, newSlot = slot });
                        break;
                    }
                }
            }
        }

        public void SwitchSlots(List<SlotChange> slots, int newSlotId, int oldSlotId, bool changeReady)
        {
            Slot newSLOT = _slots[newSlotId];
            Slot oldSLOT = _slots[oldSlotId];

            if (changeReady)
            {
                if (newSLOT.state == SlotState.READY)
                {
                    newSLOT.state = SlotState.NORMAL;
                }
                if (oldSLOT.state == SlotState.READY)
                {
                    oldSLOT.state = SlotState.NORMAL;
                }
            }

            newSLOT.SetSlotId(oldSlotId);
            oldSLOT.SetSlotId(newSlotId);

            _slots[newSlotId] = oldSLOT;
            _slots[oldSlotId] = newSLOT;
            slots.Add(new SlotChange
            {
                oldSlot = newSLOT,
                newSlot = oldSLOT
            });
        }

        public void changeSlotState(int slotId, SlotState state, bool sendInfo)
        {
            Slot slot = getSlot(slotId);
            changeSlotState(slot, state, sendInfo);
        }

        public void changeSlotState(Slot slot, SlotState state, bool sendInfo)
        {
            if (slot == null || slot.state == state)
            {
                return;
            }
            slot.state = state;
            if ((int)state == 0 || (int)state == 1)
            {
                AllUtils.ResetSlotInfo(this, slot, false);
                slot._playerId = 0;
            }
            if (sendInfo)
            {
                updateSlotsInfo();
            }
        }

        public Account getPlayerBySlot(Slot slot)
        {
            try
            {
                long id = slot._playerId;
                return id > 0 ? AccountManager.getAccount(id, true) : null;
            }
            catch
            {
                return null;
            }
        }

        public Account getPlayerBySlot(int slotId)
        {
            try
            {
                long id = _slots[slotId]._playerId;
                return id > 0 ? AccountManager.getAccount(id, true) : null;
            }
            catch
            {
                return null;
            }
        }

        public bool getPlayerBySlot(int slotId, out Account player)
        {
            try
            {
                long id = _slots[slotId]._playerId;
                player = id > 0 ? AccountManager.getAccount(id, true) : null;
                return player != null;
            }
            catch
            {
                player = null;
                return false;
            }
        }

        public bool getPlayerBySlot(Slot slot, out Account player)
        {
            try
            {
                long id = slot._playerId;
                player = id > 0 ? AccountManager.getAccount(id, true) : null;
                return player != null;
            }
            catch
            {
                player = null;
                return false;
            }
        }

        public int getTimeByMask()
        {
            return TIMES[killtime >> 4];
        }

        public int getRoundsByMask()
        {
            return ROUNDS[killtime & 15];
        }

        public int getKillsByMask()
        {
            return KILLS[killtime & 15];
        }

        public void updateSlotsInfo()
        {
            using (PROTOCOL_ROOM_GET_SLOTINFO_ACK packet = new PROTOCOL_ROOM_GET_SLOTINFO_ACK(this))
            {
                SendPacketToPlayers(packet);
            }
        }

        public bool getLeader(out Account p)
        {
            p = null;
            if (getAllPlayers().Count <= 0)
            {
                return false;
            }
            if (_leader == -1)
            {
                setNewLeader(-1, 0, -1, false);
            }
            if (_leader >= 0)
            {
                p = AccountManager.getAccount(_slots[_leader]._playerId, true);
            }
            return p != null;
        }

        public Account getLeader()
        {
            if (getAllPlayers().Count <= 0)
            {
                return null;
            }
            if (_leader == -1)
            {
                setNewLeader(-1, 0, -1, false);
            }
            return _leader == -1 ? null : AccountManager.getAccount(_slots[_leader]._playerId, true);
        }

        public void setNewLeader(int leader, int state, int oldLeader, bool updateInfo)
        {
            Monitor.Enter(_slots);
            if (leader == -1)
            {
                for (int i = 0; i < 16; ++i)
                {
                    Slot slot = _slots[i];
                    if (i != oldLeader && slot._playerId > 0 && (int)slot.state > state)
                    {
                        _leader = i;
                        break;
                    }
                }
            }
            else
            {
                _leader = leader;
            }
            if (_leader != -1)
            {
                Slot slot = _slots[_leader];
                if ((int)slot.state == 9)
                {
                    slot.state = SlotState.NORMAL;
                }
                if (updateInfo)
                {
                    updateSlotsInfo();
                }
            }
            Monitor.Exit(_slots);
        }

        public void SendPacketToPlayers(SendPacket packet)
        {
            List<Account> players = getAllPlayers();
            if (players.Count == 0)
            {
                return;
            }

            byte[] data = packet.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket)");
            foreach (Account player in players)
            {
                player.SendCompletePacket(data);
            }
        }

        public void SendPacketToPlayers(SendPacket packet, long player_id)
        {
            List<Account> players = getAllPlayers(player_id);
            if (players.Count == 0)
            {
                return;
            }

            byte[] data = packet.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket,long)");
            foreach (Account player in players)
            {
                player.SendCompletePacket(data);
            }
        }

        public void SendPacketToPlayers(SendPacket packet, SlotState state, int type)
        {
            List<Account> players = getAllPlayers(state, type);
            if (players.Count == 0)
            {
                return;
            }
            byte[] data = packet.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket,SLOT_STATE,int)");
            for (int i = 0; i < players.Count; i++)
            {
                players[i].SendCompletePacket(data);
            }
        }

        public void SendPacketToPlayers(SendPacket packet, SendPacket packet2, SlotState state, int type)
        {
            List<Account> players = getAllPlayers(state, type);
            if (players.Count == 0)
            {
                return;
            }

            byte[] data = packet.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket,SendPacket,SLOT_STATE,int)-1");
            byte[] data2 = packet2.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket,SendPacket,SLOT_STATE,int)-2");
            foreach (Account pR in players)
            {
                pR.SendCompletePacket(data);
                pR.SendCompletePacket(data2);
            }
        }

        public void SendPacketToPlayers(SendPacket packet, SlotState state, int type, int exception)
        {
            List<Account> players = getAllPlayers(state, type, exception);
            if (players.Count == 0)
            {
                return;
            }

            byte[] data = packet.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket,SLOT_STATE,int,int)");
            foreach (Account player in players)
            {
                player.SendCompletePacket(data);
            }
        }

        public void SendPacketToPlayers(SendPacket packet, SlotState state, int type, int exception, int exception2)
        {
            List<Account> players = getAllPlayers(state, type, exception, exception2);
            if (players.Count == 0)
            {
                return;
            }

            byte[] data = packet.GetCompleteBytes("Room.SendPacketToPlayers(SendPacket,SLOT_STATE,int,int,int)");
            foreach (Account player in players)
            {
                player.SendCompletePacket(data);
            }
        }

        public void RemovePlayer(Account player, bool WarnAllPlayers, int quitMotive = 0)
        {
            Slot slot;
            if (player == null || !getSlot(player._slotId, out slot))
            {
                return;
            }
            BaseRemovePlayer(player, slot, WarnAllPlayers, quitMotive);
        }

        public void RemovePlayer(Account player, Slot slot, bool WarnAllPlayers, int quitMotive = 0)
        {
            if (player == null || slot == null)
            {
                return;
            }
            BaseRemovePlayer(player, slot, WarnAllPlayers, quitMotive);
        }

        private void BaseRemovePlayer(Account player, Slot slot, bool WarnAllPlayers, int quitMotive)
        {
            Monitor.Enter(_slots);
            bool useRoomUpdate = false, hostChanged = false;
            if (player != null && slot != null)
            {
                if ((int)slot.state >= 10)
                {
                    if (_leader == slot._id)
                    {
                        int oldLeader = _leader, bestState = 1;
                        if (RoomState == RoomState.Battle)
                        {
                            bestState = 14;
                        }
                        else if (RoomState >= RoomState.Loading)
                        {
                            bestState = 9;
                        }
                        if (getAllPlayers(slot._id).Count >= 1)
                        {
                            setNewLeader(-1, bestState, _leader, false);
                        }
                        if (getPlayingPlayers(2, SlotState.READY, 1) >= 2)
                        {
                            using (PROTOCOL_BATTLE_LEAVEP2PSERVER_ACK packet = new PROTOCOL_BATTLE_LEAVEP2PSERVER_ACK(this))
                            {
                                SendPacketToPlayers(packet, SlotState.RENDEZVOUS, 1, slot._id);
                            }
                        }
                        hostChanged = true;
                    }
                    using (PROTOCOL_BATTLE_GIVEUPBATTLE_ACK packet = new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(player, quitMotive))
                    {
                        SendPacketToPlayers(packet, SlotState.READY, 1, !WarnAllPlayers ? slot._id : -1);
                    }
                    BattleLeaveSync.SendUDPPlayerLeave(this, slot._id);
                    slot.ResetSlot();
                    if (votekick != null)
                    {
                        votekick.TotalArray[slot._id] = false;
                    }
                }
                slot._playerId = 0;
                slot._equip = null;
                slot.state = SlotState.EMPTY;
                if (RoomState == RoomState.CountDown)
                {
                    if (slot._id == _leader)
                    {
                        RoomState = RoomState.Ready;
                        useRoomUpdate = true;
                        countdown.Timer = null;
                        using (PROTOCOL_BATTLE_START_COUNTDOWN_ACK packet = new PROTOCOL_BATTLE_START_COUNTDOWN_ACK(CountDownEnum.StopByHost))
                        {
                            SendPacketToPlayers(packet);
                        }
                    }
                    else if (getPlayingPlayers(slot._team, SlotState.READY, 0) == 0)
                    {
                        if (slot._id != _leader)
                        {
                            changeSlotState(_leader, SlotState.NORMAL, false);
                        }
                        StopCountDown(CountDownEnum.StopByPlayer, false);
                        useRoomUpdate = true;
                    }
                }
                else if (isPreparing())
                {
                    AllUtils.BattleEndPlayersCount(this, isBotMode());
                    if (RoomState == RoomState.Battle)
                    {
                        AllUtils.BattleEndRoundPlayersCount(this);
                    }
                }
                CheckToEndWaitingBattle(hostChanged);
                requestHost.Remove(player.player_id);
                if (vote.Timer != null && votekick != null && votekick.victimIdx == player._slotId && quitMotive != 2)
                {
                    vote.Timer = null;
                    votekick = null;
                    using (PROTOCOL_BATTLE_NOTIFY_KICKVOTE_CANCEL_ACK packet = new PROTOCOL_BATTLE_NOTIFY_KICKVOTE_CANCEL_ACK())
                    {
                        SendPacketToPlayers(packet, SlotState.BATTLE, 0);
                    }
                }
                Match match = player._match;
                if (match != null && player.matchSlot >= 0)
                {
                    match._slots[player.matchSlot].state = SlotMatchState.Normal;
                    using (PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK packet = new PROTOCOL_CLAN_WAR_REGIST_MERCENARY_ACK(match))
                    {
                        match.SendPacketToPlayers(packet);
                    }
                }
                player._room = null;
                //Logger.LogProblems("[Room.RemovePlayer] PlayerId '" + player.player_id + "' '" + player.player_name + "'; OldSlot: " + player._slotId + "; NewSlot: -1", "ErrorC");
                player._slotId = -1;
                player._status.updateRoom(255);
                AllUtils.syncPlayerToClanMembers(player);
                AllUtils.syncPlayerToFriends(player, false);
                player.updateCacheInfo();
            }
            updateSlotsInfo();
            if (useRoomUpdate)
            {
                updateRoomInfo();
            }
            Monitor.Exit(_slots);
        }

        public int addPlayer(Account p)
        {
            lock (_slots)
            {
                for (int i = 0; i < 16; i++)
                {
                    Slot slot = _slots[i];
                    if (slot._playerId == 0 && (int)slot.state == 0)
                    {
                        slot._playerId = p.player_id;
                        slot.state = SlotState.NORMAL;
                        p._room = this;
                        //Logger.LogProblems("[Room.AddPlayer] PlayerId '" + p.player_id + "' '" + p.player_name + "'; OldSlot: " + p._slotId + "; NewSlot: " + i, "ErrorC");
                        p._slotId = i;
                        slot._equip = p._equip;
                        p._status.updateRoom((byte)_roomId);
                        AllUtils.syncPlayerToClanMembers(p);
                        AllUtils.syncPlayerToFriends(p, false);
                        p.updateCacheInfo();
                        return i;
                    }
                }
            }
            return -1;
        }

        public int addPlayer(Account p, int teamIdx)
        {
            int[] array = GetTeamArray(teamIdx);
            lock (_slots)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    int SlotIdx = array[i];
                    Slot slot = _slots[SlotIdx];
                    if (slot._playerId == 0 && (int)slot.state == 0)
                    {
                        slot._playerId = p.player_id;
                        slot.state = SlotState.NORMAL;
                        p._room = this;
                        //Logger.LogProblems("[Room.AddPlayer] PlayerId '" + p.player_id + "' '" + p.player_name + "'; OldSlot: " + p._slotId + "; NewSlot: " + i, "ErrorC");
                        p._slotId = SlotIdx;
                        slot._equip = p._equip;
                        p._status.updateRoom((byte)_roomId);
                        AllUtils.syncPlayerToClanMembers(p);
                        AllUtils.syncPlayerToFriends(p, false);
                        p.updateCacheInfo();
                        return SlotIdx;
                    }
                }
            }
            return -1;
        }

        public int[] GetTeamArray(int index)
        {
            return (index == 0 ? RED_TEAM : BLUE_TEAM);
        }

        public List<Account> getAllPlayers(SlotState state, int type)
        {
            List<Account> list = new List<Account>();
            lock (_slots)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    Slot slot = _slots[i];
                    long id = slot._playerId;
                    if (id > 0 && (type == 0 && slot.state == state || type == 1 && slot.state > state))
                    {
                        Account player = AccountManager.getAccount(id, true);
                        if (player != null && player._slotId != -1)
                        {
                            list.Add(player);
                        }
                    }
                }
            }
            return list;
        }

        public List<Account> getAllPlayers(SlotState state, int type, int exception)
        {
            List<Account> list = new List<Account>();
            lock (_slots)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    Slot slot = _slots[i];
                    long id = slot._playerId;
                    if (id > 0 && i != exception && (type == 0 && slot.state == state || type == 1 && slot.state > state))
                    {
                        Account player = AccountManager.getAccount(id, true);
                        if (player != null && player._slotId != -1)
                        {
                            list.Add(player);
                        }
                    }
                }
            }
            return list;
        }

        public List<Account> getAllPlayers(SlotState state, int type, int exception, int exception2)
        {
            List<Account> list = new List<Account>();
            lock (_slots)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    Slot slot = _slots[i];
                    long id = slot._playerId;
                    if (id > 0 && i != exception && i != exception2 && (type == 0 && slot.state == state || type == 1 && slot.state > state))
                    {
                        Account player = AccountManager.getAccount(id, true);
                        if (player != null && player._slotId != -1)
                        {
                            list.Add(player);
                        }
                    }
                }
            }
            return list;
        }

        public List<Account> getAllPlayers(int exception)
        {
            List<Account> list = new List<Account>();
            lock (_slots)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    long id = _slots[i]._playerId;
                    if (id > 0 && i != exception)
                    {
                        Account player = AccountManager.getAccount(id, true);
                        if (player != null && player._slotId != -1)
                        {
                            list.Add(player);
                        }
                    }
                }
            }
            return list;
        }

        public List<Account> getAllPlayers(long exception)
        {
            List<Account> list = new List<Account>();
            lock (_slots)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    long id = _slots[i]._playerId;
                    if (id > 0 && id != exception)
                    {
                        Account player = AccountManager.getAccount(id, true);
                        if (player != null && player._slotId != -1)
                        {
                            list.Add(player);
                        }
                    }
                }
            }
            return list;
        }

        public List<Account> getAllPlayers()
        {
            List<Account> list = new List<Account>();
            lock (_slots)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    long id = _slots[i]._playerId;
                    if (id > 0)
                    {
                        Account player = AccountManager.getAccount(id, true);
                        if (player != null && player._slotId != -1)
                        {
                            list.Add(player);
                        }
                    }
                }
            }
            return list;
        }

        public int getPlayingPlayers(int team, bool inBattle)
        {
            int players = 0;
            lock (_slots)
            {
                foreach (Slot slot in _slots)
                {
                    if (slot._playerId > 0 && (slot._team == team || team == 2) && (inBattle && (int)slot.state == 13 && !slot.espectador || !inBattle && (int)slot.state >= 10))
                    {
                        players++;
                    }
                }
            }
            return players;
        }

        public int getPlayingPlayers(int team, SlotState state, int type)
        {
            int players = 0;
            lock (_slots)
            {
                foreach (Slot slot in _slots)
                {
                    if (slot._playerId > 0 && (type == 0 && slot.state == state || type == 1 && slot.state > state) && (team == 2 || slot._team == team))
                    {
                        players++;
                    }
                }
            }
            return players;
        }

        public int getPlayingPlayers(int team, SlotState state, int type, int exception)
        {
            int players = 0;
            lock (_slots)
            {
                for (int i = 0; i < 16; i++)
                {
                    Slot slot = _slots[i];
                    if (i != exception && slot._playerId > 0 && (type == 0 && slot.state == state || type == 1 && slot.state > state) && (team == 2 || slot._team == team))
                    {
                        players++;
                    }
                }
            }
            return players;
        }

        public void getPlayingPlayers(bool inBattle, out int RedPlayers, out int BluePlayers)
        {
            RedPlayers = 0;
            BluePlayers = 0;
            lock (_slots)
            {
                foreach (Slot slot in _slots)
                {
                    if (slot._playerId > 0 && (inBattle && (int)slot.state == 15 && !slot.espectador || !inBattle && (int)slot.state >= 11))
                    {
                        if (slot._team == 0)
                        {
                            RedPlayers++;
                        }
                        else
                        {
                            BluePlayers++;
                        }
                    }
                }
            }
        }

        public void getPlayingPlayers(bool inBattle, out int RedPlayers, out int BluePlayers, out int RedDeaths, out int BlueDeaths)
        {
            RedPlayers = 0;
            BluePlayers = 0;
            RedDeaths = 0;
            BlueDeaths = 0;
            lock (_slots)
            {
                foreach (Slot slot in _slots)
                {
                    if (slot._deathState.HasFlag(DeadEnum.Dead))
                    {
                        if (slot._team == 0)
                        {
                            RedDeaths++;
                        }
                        else
                        {
                            BlueDeaths++;
                        }
                    }
                    if (slot._playerId > 0 && (inBattle && (int)slot.state == 15 && !slot.espectador || !inBattle && (int)slot.state >= 11))
                    {
                        if (slot._team == 0)
                        {
                            RedPlayers++;
                        }
                        else
                        {
                            BluePlayers++;
                        }
                    }
                }
            }
        }

        public void CheckToEndWaitingBattle(bool host)
        {
            //Logger.warning("state: " + _roomState + "; leader: " + _slots[_leader].state);
            if ((RoomState == RoomState.CountDown || RoomState == RoomState.Loading || RoomState == RoomState.Rendezvous) && (host || _slots[_leader].state == SlotState.BATTLE_READY))
            {
                AllUtils.EndBattleNoPoints(this);
            }
        }

        public void SpawnReadyPlayers()
        {
            lock (_slots)
            {
                BaseSpawnReadyPlayers(isBotMode());
            }
        }

        public void SpawnReadyPlayers(bool isBotMode)
        {
            lock (_slots)
            {
                BaseSpawnReadyPlayers(isBotMode);
            }
        }

        private void BaseSpawnReadyPlayers(bool isBotMode)
        {
            bool dinoStart = false;
            using (PROTOCOL_BATTLE_COUNT_DOWN_ACK packet5 = new PROTOCOL_BATTLE_COUNT_DOWN_ACK(3))
            {
                if (RoomState == RoomState.PreBattle && Countdown == false && isBotMode == false)
                {
                    Countdown = true;
                    SendPacketToPlayers(packet5);
                }
                countdownstart.Start(3250, (callbackState) =>
                {
                    try
                    {
                        DateTime date = DateTime.Now;
                        foreach (Slot slot in _slots)
                        {
                            if ((int)slot.state == 14 && slot.isPlaying == 0 && slot._playerId > 0)
                            {
                                slot.isPlaying = 1;
                                slot.startTime = date;
                                slot.state = SlotState.BATTLE;
                                if (RoomState == RoomState.Battle && (RoomType == RoomType.Bomb || RoomType == RoomType.Annihilation || RoomType == RoomType.Convoy))
                                {
                                    slot.espectador = true;
                                }
                            }
                        }
                        updateSlotsInfo();
                        List<int> dinos = AllUtils.getDinossaurs(this, false, -1);
                        if (RoomState == RoomState.PreBattle)
                        {
                            BattleStart = RoomType == RoomType.Bomb || RoomType == RoomType.CrossCounter || RoomType == RoomType.Ace ? date.AddMinutes(5) : date;
                            SetSpecialStage();
                        }
                        using (PROTOCOL_BATTLE_MISSION_ROUND_PRE_START_ACK packet = new PROTOCOL_BATTLE_MISSION_ROUND_PRE_START_ACK(this, dinos, isBotMode))
                        {
                            using (PROTOCOL_BATTLE_MISSION_ROUND_START_ACK packet2 = new PROTOCOL_BATTLE_MISSION_ROUND_START_ACK(this))
                            {
                                using (PROTOCOL_BATTLE_RECORD_ACK packet3 = new PROTOCOL_BATTLE_RECORD_ACK(this))
                                {
                                    byte[] data = packet.GetCompleteBytes("Room.BaseSpawnReadyPlayers-1");
                                    byte[] data2 = packet2.GetCompleteBytes("Room.BaseSpawnReadyPlayers-2");
                                    byte[] data3 = packet3.GetCompleteBytes("Room.BaseSpawnReadyPlayers-3");
                                    foreach (Slot slot in _slots)
                                    {
                                        Account player;
                                        if (slot.state == SlotState.BATTLE && slot.isPlaying == 1 && getPlayerBySlot(slot, out player))
                                        {
                                            slot.isPlaying = 2;
                                            if (MaskActive || GameRuleActive)
                                            {
                                                slot._equip.face = 1000700000;
                                                slot._equip._helmet = 1000800000;
                                            }
                                            else
                                            {
                                                slot._equip.face = player._equip.face;
                                                slot._equip._helmet = player._equip._helmet;
                                            }
                                            if (RoomState == RoomState.PreBattle)
                                            {
                                                using (PROTOCOL_BATTLE_STARTBATTLE_ACK packet4 = new PROTOCOL_BATTLE_STARTBATTLE_ACK(slot, player, dinos, isBotMode, true))
                                                {
                                                    SendPacketToPlayers(packet4, SlotState.READY, 1);
                                                }
                                                player.SendCompletePacket(data);
                                                if (RoomType == RoomType.Boss || RoomType == RoomType.CrossCounter)
                                                {
                                                    dinoStart = true;
                                                }
                                                else
                                                {
                                                    player.SendCompletePacket(data2);
                                                }
                                            }
                                            else if (RoomState == RoomState.Battle)
                                            {
                                                using (PROTOCOL_BATTLE_STARTBATTLE_ACK packet4 = new PROTOCOL_BATTLE_STARTBATTLE_ACK(slot, player, dinos, isBotMode, false))
                                                {
                                                    SendPacketToPlayers(packet4, SlotState.READY, 1);
                                                }
                                                if (RoomType == RoomType.Bomb || RoomType == RoomType.Annihilation || RoomType == RoomType.Ace || RoomType == RoomType.Convoy)
                                                {
                                                    GameSync.SendUDPPlayerSync(this, slot, 0, 1);
                                                }
                                                else
                                                {
                                                    player.SendCompletePacket(data);
                                                }
                                                player.SendCompletePacket(data2);
                                                player.SendCompletePacket(data3);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.warning("[Room.StartCountDown] " + ex.ToString());
                    }
                    lock (callbackState)
                    {
                        countdownstart.Timer = null;
                    }
                    if (RoomState == RoomState.PreBattle)
                    {
                        RoomState = RoomState.Battle;
                        updateRoomInfo();
                    }
                    if (dinoStart)
                    {
                        StartDinoRound();
                    }
                });
            }
        }

        private void StartDinoRound()
        {
            round.Start(5250, (callbackState) =>
            {
                if (RoomState == RoomState.Battle)
                {
                    using (PROTOCOL_BATTLE_MISSION_ROUND_START_ACK packet = new PROTOCOL_BATTLE_MISSION_ROUND_START_ACK(this))
                    {
                        SendPacketToPlayers(packet, SlotState.BATTLE, 0);
                    }
                    swapRound = false;
                }
                lock (callbackState)
                {
                    round.Timer = null;
                }
            });
        }
    }
}