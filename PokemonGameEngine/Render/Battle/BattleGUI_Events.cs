using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Packets;
using System;
using System.Threading;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed partial class BattleGUI
    {
        // Battle thread
        private IPBEPacket _newPacket;
        private PBEBattleState? _newState;
        private readonly ManualResetEvent _resumeProcessing = new(false);

        private void SinglePlayerBattle_OnNewEvent(PBEBattle _, IPBEPacket packet)
        {
            _newPacket = packet;
            _resumeProcessing.Reset(); // Pause battle thread
            _resumeProcessing.WaitOne(); // Wait for permission to continue
            // TODO: Will keep the game alive even if close is pressed
        }
        private void SinglePlayerBattle_OnStateChanged(PBEBattle battle)
        {
            _newState = battle.BattleState;
            // Let this thread die; we will create a new one if the battle is to continue
        }

        /// <summary>
        /// Using this is necessary to prevent the battle state from changing on the main thread.
        /// Whatever GL calls we make must be on the main thread though, which is why we delegate the packets and states back.
        /// </summary>
        private static void CreateBattleThread(ThreadStart start)
        {
            new Thread(start) { Name = "Battle Thread" }.Start(); // TODO: Need to implement cancellation tokens in PBE... thread causes the game to hang when closed
        }
        private void ResumeBattleThread()
        {
            _resumeProcessing.Set();
        }

        private bool HandleNewPacket()
        {
            if (_newPacket is null)
            {
                return false;
            }
            IPBEPacket packet = _newPacket;
            _newPacket = null;
            ProcessPacket(packet);
            return true;
        }
        private void HandleNewBattleState()
        {
            if (_newState is null)
            {
                return;
            }
            PBEBattleState s = _newState.Value;
            _newState = null;
            switch (s)
            {
                case PBEBattleState.Ended: OnBattleEnded(); break;
                case PBEBattleState.ReadyToRunSwitches: CreateBattleThread(Battle.RunSwitches); break;
                case PBEBattleState.ReadyToRunTurn: CreateBattleThread(Battle.RunTurn); break;
            }
        }
        private void HandleNewEvents()
        {
            if (!HandleNewPacket())
            {
                HandleNewBattleState();
            }
        }

        private void ShowPacketMessageThenResumeBattleThread(IPBEPacket packet)
        {
            string message = null;
            switch (packet)
            {
                case PBEFleeFailedPacket ffp:
                {
                    PBETrainer t = ffp.PokemonTrainer;
                    if (t == _trainer)
                    {
                        message = "Couldn't get away!";
                        break;
                    }
                    break;
                }
                case PBEBattleResultPacket brp:
                {
                    switch (brp.BattleResult)
                    {
                        case PBEBattleResult.Team0Win:
                        {
                            if (Battle.BattleType == PBEBattleType.Trainer)
                            {
                                message = _trainerDefeatText;
                            }
                            break;
                        }
                    }
                    break;
                }
            }

            // No custom message, so get the default one
            if (message is null)
            {
                message = PBEBattle.GetDefaultMessage(Battle, packet, userTrainer: _trainer);
            }
            // No message, so return
            if (string.IsNullOrEmpty(message))
            {
                ResumeBattleThread();
                return;
            }
            // Print message
            SetMessage(message, ResumeBattleThread);
        }

        // These methods handle the form changing animation
        private void StartRevealIfNotSubstituted(IPBEPacket packet, BattlePokemon bPkmn, Action<BattlePokemon> reveal)
        {
            if (bPkmn.PBEPkmn.Status2.HasFlag(PBEStatus2.Substitute))
            {
                ShowPacketMessageThenResumeBattleThread(packet); // Substitute exists, don't animate
            }
            else
            {
                var data = new TaskData_ChangeSprite(packet, reveal, bPkmn);
                _tasks.Add(Task_ChangeSprite_Start, 0, data: data);
            }
        }
        private void RevealForm(BattlePokemon bPkmn)
        {
            SetSeen(bPkmn);
            bPkmn.UpdateMini();
            bPkmn.UpdateSprite(img: true, imgIfSubstituted: false);
            bPkmn.UpdateInfoBar(); // Update info because HP change is possible in PBE with custom data
        }
        private void RevealDisguise(BattlePokemon bPkmn)
        {
            bPkmn.UpdateDisguisedPID();
            SetSeen(bPkmn);
            bPkmn.UpdateMini();
            bPkmn.UpdateSprite(img: true, imgIfSubstituted: false);
            bPkmn.UpdateInfoBar();
        }
        private void RevealTransform(BattlePokemon bPkmn)
        {
            bPkmn.UpdateMini();
            bPkmn.UpdateSprite(img: true, imgIfSubstituted: false);
        }

        private void MovePokemonToEmptyPos(PBEBattlePokemon pbePkmn)
        {
            BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
            PkmnPosition oldPos = bPkmn.DetachPos();
            oldPos.Clear();
            PkmnPosition newPos = GetPkmnPosition(pbePkmn.Team.Id, pbePkmn.FieldPosition);
            bPkmn.AttachPos(newPos);
            bPkmn.UpdateSprite(img: true, imgIfSubstituted: true, visibility: true, color: true);
            newPos.InfoVisible = true;
        }

        private void ProcessPacket(IPBEPacket packet)
        {
            switch (packet)
            {
                case PBEMoveLockPacket _:
                case PBEMovePPChangedPacket _:
                case PBEIllusionPacket _:
                case PBETransformPacket _:
                case PBEBattlePacket _:
                case PBETurnBeganPacket _:
                {
                    ResumeBattleThread(); // No need to wait or show messages
                    return;
                }
                case PBEActionsRequestPacket arp:
                {
                    PBETrainer t = arp.Trainer;
                    if (t == _trainer)
                    {
                        ActionsBuilder = new ActionsBuilder(t);
                        ActionsBuilder.ActionsLoop(); // Init loop
                    }
                    else
                    {
                        // If the team is wild, no flees are allowed by default
                        // Check IsWild instead of Battle type, since we can have an ai partner trainer
                        if (t.IsWild)
                        {
                            CreateBattleThread(() => _wildAI.CreateActions(false));
                        }
                        else
                        {
                            CreateBattleThread(_ais[t.Id].CreateActions);
                        }
                    }
                    ResumeBattleThread(); // No need to wait
                    return;
                }
                case PBESwitchInRequestPacket sirp:
                {
                    PBETrainer t = sirp.Trainer;
                    if (t == _trainer)
                    {
                        SwitchesBuilder = new SwitchesBuilder(sirp.Amount);
                        InitFadeToPartyForReplacement(); // Loop will begin in PartyGUI
                    }
                    else
                    {
                        CreateBattleThread(_ais[t.Id].CreateSwitches);
                    }
                    ResumeBattleThread(); // No need to wait
                    return;
                }
                case PBEAutoCenterPacket acp:
                {
                    MovePokemonToEmptyPos(acp.Pokemon0Trainer.GetPokemon(acp.Pokemon0));
                    MovePokemonToEmptyPos(acp.Pokemon1Trainer.GetPokemon(acp.Pokemon1));
                    break;
                }
                case PBEPkmnEXPChangedPacket pecp:
                {
                    PBEBattlePokemon pbePkmn = pecp.PokemonTrainer.GetPokemon(pecp.Pokemon);
                    if (pbePkmn.FieldPosition != PBEFieldPosition.None)
                    {
                        BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                        bPkmn.UpdateInfoBar();
                    }
                    break;
                }
                case PBEPkmnFaintedPacket pfp:
                {
                    PBEBattlePokemon pbePkmn = pfp.PokemonTrainer.GetPokemon(pfp.Pokemon);
                    BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                    PkmnPosition pos = bPkmn.DetachPos();
                    pos.Clear();
                    if (pbePkmn.Trainer == _trainer)
                    {
                        UpdateFriendshipForFaint(bPkmn);
                    }
                    PlayCry(pbePkmn);
                    break;
                }
                case PBEPkmnFormChangedPacket pfcp:
                {
                    PBEBattlePokemon pbePkmn = pfcp.PokemonTrainer.GetPokemon(pfcp.Pokemon);
                    BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                    StartRevealIfNotSubstituted(packet, bPkmn, RevealForm);
                    return; // Don't resume battle thread yet
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pbePkmn = phcp.PokemonTrainer.GetPokemon(phcp.Pokemon);
                    BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                    bPkmn.UpdateAnimationSpeed();
                    bPkmn.UpdateInfoBar();
                    break;
                }
                case PBEPkmnLevelChangedPacket plcp:
                {
                    PBEBattlePokemon pbePkmn = plcp.PokemonTrainer.GetPokemon(plcp.Pokemon);
                    BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                    if (pbePkmn.FieldPosition != PBEFieldPosition.None)
                    {
                        bPkmn.UpdateInfoBar();
                    }
                    UpdateFriendshipForLevelUp(bPkmn);
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.GetPokemon(s1p.Status1Receiver);
                    BattlePokemon bPkmn = GetBattlePokemon(status1Receiver);
                    bPkmn.UpdateAnimationSpeed();
                    bPkmn.UpdateSprite(color: true);
                    bPkmn.UpdateInfoBar();
                    break;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEBattlePokemon status2Receiver = s2p.Status2ReceiverTrainer.GetPokemon(s2p.Status2Receiver);
                    BattlePokemon bPkmn = GetBattlePokemon(status2Receiver);
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne:
                        case PBEStatus2.ShadowForce:
                        case PBEStatus2.Underground:
                        case PBEStatus2.Underwater:
                        {
                            bPkmn.UpdateSprite(visibility: true);
                            break;
                        }
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended:
                                {
                                    StartRevealIfNotSubstituted(packet, bPkmn, RevealDisguise);
                                    return; // Don't resume battle thread yet
                                }
                            }
                            break;
                        }
                        case PBEStatus2.Substitute:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Ended:
                                {
                                    bPkmn.UpdateSprite(img: true, imgIfSubstituted: true, visibility: true, color: true);
                                    break;
                                }
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                {
                                    StartRevealIfNotSubstituted(packet, bPkmn, RevealTransform);
                                    return; // Don't resume battle thread yet
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
                case PBEPkmnSwitchInPacket psip:
                {
                    PBETrainer trainer = psip.Trainer;
                    foreach (PBEPkmnAppearedInfo info in psip.SwitchIns)
                    {
                        PBEBattlePokemon pbePkmn = trainer.GetPokemon(info.Pokemon);
                        BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                        bPkmn.UpdateDisguisedPID();
                        SetSeen(bPkmn);
                        PkmnPosition pos = GetPkmnPosition(pbePkmn.Team.Id, pbePkmn.FieldPosition);
                        bPkmn.AttachPos(pos);
                        bPkmn.UpdateMini();
                        bPkmn.UpdateSprite(img: true, imgIfSubstituted: true, visibility: true, color: true); // Also sets animation speed
                        bPkmn.UpdateInfoBar();
                        pos.InfoVisible = true;
                        PlayCry(pbePkmn);
                    }
                    break;
                }
                case PBEPkmnSwitchOutPacket psop:
                {
                    PBEBattlePokemon pbePkmn = psop.PokemonTrainer.GetPokemon(psop.Pokemon);
                    BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                    bPkmn.UpdateMini();
                    PkmnPosition pos = bPkmn.DetachPos();
                    pos.Clear();
                    break;
                }
                case PBEWildPkmnAppearedPacket wpap:
                {
                    PBETrainer trainer = Battle.Teams[1].Trainers[0];
                    foreach (PBEPkmnAppearedInfo info in wpap.Pokemon)
                    {
                        PBEBattlePokemon pbePkmn = trainer.GetPokemon(info.Pokemon);
                        BattlePokemon bPkmn = GetBattlePokemon(pbePkmn);
                        bPkmn.UpdateDisguisedPID();
                        SetSeen(bPkmn);
                        bPkmn.UpdateInfoBar(); // Only update and set the info to visible because the sprite is already loaded and visible
                        PkmnPosition pos = GetPkmnPosition(1, pbePkmn.FieldPosition); // pos already attached for wild BattlePokemon
                        pos.InfoVisible = true;
                        PlayCry(pbePkmn);
                    }
                    break;
                }
                /*
                case PBEWeatherPacket wp:
                {
                    switch (wp.WeatherAction)
                    {
                        case PBEWeatherAction.Added:
                        case PBEWeatherAction.Ended: BattleView.Field.UpdateWeather(); break;
                        case PBEWeatherAction.CausedDamage: break;
                    }
                    break;
                }*/
            }

            ShowPacketMessageThenResumeBattleThread(packet);
        }
    }
}
