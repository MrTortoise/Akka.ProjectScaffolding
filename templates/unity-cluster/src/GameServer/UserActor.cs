﻿using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimServer;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    [ResponsiveException(typeof(ResultException))]
    public class UserActor : InterfacedActor, IUser
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private ActorBoundChannelRef _channel;
        private long _id;
        private TrackableUserContext _userContext;
        private UserEventObserver _userEventObserver;

        public UserActor(ClusterNodeContext clusterContext, ActorBoundChannelRef channel,
                         long id, TrackableUserContext userContext, IUserEventObserver observer)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _channel = channel;
            _id = id;
            _userContext = userContext;
            _userEventObserver = (UserEventObserver)observer;
        }

        Task IUser.SetNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
                throw new ResultException(ResultCodeType.NicknameInvalid);

            _userContext.Data.Nickname = nickname;
            FlushUserContext();

            return Task.CompletedTask;
        }

        Task IUser.AddNote(int id, string note)
        {
            if (string.IsNullOrEmpty(note))
                throw new ResultException(ResultCodeType.NicknameInvalid);

            if (_userContext.Notes.ContainsKey(id))
                throw new ResultException(ResultCodeType.NoteDuplicate);

            _userContext.Notes.Add(id, note);
            FlushUserContext();

            return Task.CompletedTask;
        }

        Task IUser.RemoveNote(int id)
        {
            if (_userContext.Notes.Remove(id) == false)
                throw new ResultException(ResultCodeType.NoteNotFound);

            FlushUserContext();

            return Task.CompletedTask;
        }

        private void FlushUserContext()
        {
            if (_userEventObserver != null)
                _userEventObserver.UserContextChange(_userContext.Tracker);
            _userContext.Tracker = new TrackableUserContextTracker();
        }
    }
}
