/* thanks to the devs of the deathlink and head2head mod for making their mods open source, rly helped with this and the data :D */
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Components;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.PvPDash.Hooks;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.PvPDash.IO;
public class CNetComm : GameComponent
{
    public static CNetComm Instance { get; private set; }

    public delegate void OnConnectedHandler(CelesteNetClientContext cxt);
    public static event OnConnectedHandler OnConnected;

    public delegate void OnDisonnectedHandler(CelesteNetConnection con);
    public static event OnDisonnectedHandler OnDisconnected;


    public CelesteNetClientContext CnetContext { get { return CelesteNetClientModule.Instance?.Context; } }

    public CelesteNetClient CnetClient { get { return CelesteNetClientModule.Instance?.Client; } }
    public bool IsConnected { get { return CnetClient?.Con?.IsConnected ?? false; } }
    public uint? CnetID { get { return IsConnected ? (uint?)CnetClient?.PlayerInfo?.ID : null; } }
    /* idk */
    public long MaxPacketSize { get { return CnetClient?.Con is CelesteNetTCPUDPConnection connection ? (connection.ConnectionSettings?.MaxPacketSize ?? 2048) : 2048; } }

    public DataChannelList.Channel CurrentChannel
    {
        get
        {
            if (!IsConnected) return null;
            KeyValuePair<Type, CelesteNetGameComponent> listComp = CnetContext.Components.FirstOrDefault((KeyValuePair<Type, CelesteNetGameComponent> kvp) =>
            {
                return kvp.Key == typeof(CelesteNetPlayerListComponent);
            });
            if (listComp.Equals(default(KeyValuePair<Type, CelesteNetGameComponent>))) return null;
            CelesteNetPlayerListComponent comp = listComp.Value as CelesteNetPlayerListComponent;
            DataChannelList.Channel[] list = comp.Channels?.List;
            return list?.FirstOrDefault(c => c.Players.Contains(CnetClient.PlayerInfo.ID));
        }
    }

    private ConcurrentQueue<Action> updateQueue = new ConcurrentQueue<Action>();

    public CNetComm(Game game)
      : base(game)
    {
        Instance = this;
        Disposed += OnComponentDisposed;
        CelesteNetClientContext.OnStart += OnCNetClientContextStart;
        CelesteNetClientContext.OnDispose += OnCNetClientContextDispose;
    }

    private void OnComponentDisposed(object sender, EventArgs args)
    {
        CelesteNetClientContext.OnStart -= OnCNetClientContextStart;
        CelesteNetClientContext.OnDispose -= OnCNetClientContextDispose;
    }

    private void OnCNetClientContextStart(CelesteNetClientContext cxt)
    {
        CnetClient.Data.RegisterHandlersIn(this);
        CnetClient.Con.OnDisconnect += OnDisconnect;
        updateQueue.Enqueue(() => OnConnected?.Invoke(cxt));
    }

    private void OnCNetClientContextDispose(CelesteNetClientContext cxt)
    {
        // CnetClient is null here
    }

    private void OnDisconnect(CelesteNetConnection con)
    {
        updateQueue.Enqueue(() => OnDisconnected?.Invoke(con));
    }

    public override void Update(GameTime gameTime)
    {
        ConcurrentQueue<Action> queue = updateQueue;
        updateQueue = new ConcurrentQueue<Action>();
        foreach (Action act in queue)
        {
            act();
        }
        base.Update(gameTime);
    }

    internal void Send<T>(T data) where T : DataType<T>
    {
        CnetClient.Send(data);
    }

    public void Handle(CelesteNetConnection con, Data.TeamUpdateRequest data)
    {
        if (!CurrentChannel.Name.Equals(data.cNetChannel)) { return; }
        updateQueue.Enqueue(() =>
        {
            Instance.Send(new Data.TeamUpdate());
            PlayerUpdateHook.addOrChangePlayerTeam(data.playerID, data.team);
        });
    }

    public void Handle(CelesteNetConnection con, Data.TeamUpdate data)
    {
        if (!CurrentChannel.Name.Equals(data.cNetChannel)) { return; }
        updateQueue.Enqueue(() => PlayerUpdateHook.addOrChangePlayerTeam(data.playerID, data.team));
    }
}
