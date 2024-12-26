using System;
using System.Collections.Generic;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.Communication {
    // This file contains EventArgs definition for
    // different callbacks used in Arcor2Client

    public class StringEventArgs : EventArgs {
        public string Data { get; set; }

        public StringEventArgs(string data) {
            Data = data;
        }
    }

    public class StringListEventArgs : EventArgs {
        public IList<string> Data { get; set; }

        public StringListEventArgs(IList<string> data) {
            Data = data;
        }
    }

    public class FloatEventArgs : EventArgs {
        public float Data { get; set; }

        public FloatEventArgs(float data) {
            Data = data;
        }
    }

    public class ProjectMetaEventArgs : EventArgs {
        public string Name { get; set; }
        public string Id { get; set; }

        public ProjectMetaEventArgs(string id, string name) {
            Id = id;
            Name = name;
        }
    }

    public class BareProjectEventArgs : EventArgs {
        public BareProject Project { get; set; }

        public BareProjectEventArgs(BareProject project) {
            Project = project;
        }
    }

    public class BareSceneEventArgs : EventArgs {
        public BareScene Scene { get; set; }

        public BareSceneEventArgs(BareScene scene) {
            Scene = scene;
        }
    }

    public class BareActionEventArgs : EventArgs {
        public BareAction Action { get; set; }

        public BareActionEventArgs(BareAction action) {
            Action = action;
        }
    }

    public class ActionModelEventArgs : EventArgs {
        public Action Action { get; set; }

        public ActionModelEventArgs(Action action) {
            Action = action;
        }
    }

    public class BareActionPointEventArgs : EventArgs {
        public BareActionPoint ActionPoint { get; set; }

        public BareActionPointEventArgs(BareActionPoint actionPoint) {
            ActionPoint = actionPoint;
        }
    }

    public class ProjectActionPointEventArgs : EventArgs {
        public ActionPoint ActionPoint { get; set; }

        public ProjectActionPointEventArgs(ActionPoint actionPoint) {
            ActionPoint = actionPoint;
        }
    }

    public class ActionPointEventArgs : EventArgs {
        public ActionPoint ActionPoint { get; set; }

        public ActionPointEventArgs(ActionPoint actionPoint) {
            ActionPoint = actionPoint;
        }
    }

    public class RobotEefUpdatedEventArgs : EventArgs {
        public RobotEefData Data { get; set; }

        public RobotEefUpdatedEventArgs(RobotEefData data) {
            Data = data;
        }
    }

    public class RobotJointsUpdatedEventArgs {
        public RobotJointsData Data { get; set; }

        public RobotJointsUpdatedEventArgs(RobotJointsData data) {
            Data = data;
        }
    }

    public class LogicItemChangedEventArgs {
        public LogicItem Data { get; set; }

        public LogicItemChangedEventArgs(LogicItem data) {
            Data = data;
        }
    }

    public class ShowMainScreenEventArgs {
        public ShowMainScreenData Data { get; set; }
        public ShowMainScreenEventArgs(ShowMainScreenData data) {
            Data = data;
        }
    }


    public class ActionPointOrientationEventArgs : EventArgs {
        public NamedOrientation Data { get; set; }
        public string ActionPointId { get; set; }

        public ActionPointOrientationEventArgs(NamedOrientation data, string actionPointId) {
            Data = data;
            ActionPointId = actionPointId;
        }
    }

    public class RobotJointsEventArgs : EventArgs {
        public ProjectRobotJoints Data { get; set; }
        public string ActionPointId { get; set; }

        public RobotJointsEventArgs(ProjectRobotJoints data, string actionPointId) {
            Data = data;
            ActionPointId = actionPointId;
        }
    }

    public class ActionPointOrientationAddedEventArgs : EventArgs {
        public string ActionPointId { get; set; }
        public NamedOrientation Orientation { get; set; }

        public ActionPointOrientationAddedEventArgs(string actionPointId, NamedOrientation orientation) {
            ActionPointId = actionPointId;
            Orientation = orientation;
        }
    }

    public class ActionPointJointsAddedEventArgs : EventArgs {
        public string ActionPointId { get; set; }
        public ProjectRobotJoints Joints { get; set; }

        public ActionPointJointsAddedEventArgs(string actionPointId, ProjectRobotJoints joints) {
            ActionPointId = actionPointId;
            Joints = joints;
        }
    }

    public class RobotUrdfModelArgs : EventArgs {
        public string RobotType { get; set; }

        public RobotUrdfModelArgs(string robotType) {
            RobotType = robotType;
        }
    }

    public class RobotMoveToPoseEventArgs : EventArgs {
        public RobotMoveToPose Event { get; set; }

        public RobotMoveToPoseEventArgs(RobotMoveToPose @event) {
            Event = @event;
        }
    }

    public class RobotMoveToJointsEventArgs : EventArgs {

        public RobotMoveToJoints Event { get; set; }

        public RobotMoveToJointsEventArgs(RobotMoveToJoints @event) {
            Event = @event;
        }
    }

    public class RobotMoveToActionPointJointsEventArgs : EventArgs {

        public RobotMoveToActionPointJoints Event { get; set; }

        public RobotMoveToActionPointJointsEventArgs(RobotMoveToActionPointJoints @event) {
            Event = @event;
        }
    }

    public class RobotMoveToActionPointOrientationEventArgs : EventArgs {

        public RobotMoveToActionPointOrientation Event { get; set; }

        public RobotMoveToActionPointOrientationEventArgs(RobotMoveToActionPointOrientation @event) {
            Event = @event;
        }
    }

    public class SceneStateEventArgs : EventArgs {

        public SceneStateData Event { get; set; }

        public SceneStateEventArgs(SceneStateData @event) {
            Event = @event;
        }
    }

    public class ParameterEventArgs : EventArgs {

        public Parameter Parameter { get; set; }
        public string ObjectId { get; set; }

        public ParameterEventArgs(string objectId, Parameter @event) {
            Parameter = @event;
            ObjectId = objectId;
        }
    }

    public class ObjectTypeEventArgs : EventArgs {
        public ObjectTypeMeta ObjectType { get; set; }

        public ObjectTypeEventArgs(ObjectTypeMeta objectType) {
            ObjectType = objectType;
        }
    }

    public class ObjectTypesEventArgs : EventArgs {
        public List<ObjectTypeMeta> ObjectTypes { get; set; }

        public ObjectTypesEventArgs(List<ObjectTypeMeta> objectTypes) {
            ObjectTypes = objectTypes;
        }
    }

    public class ObjectLockingEventArgs : EventArgs {
        public IList<string> ObjectIds { get; set; }
        public bool Locked { get; set; }
        public string Owner { get; set; }

        public ObjectLockingEventArgs(IList<string> objectIds, bool locked, string owner) {
            ObjectIds = objectIds;
            Locked = locked;
            Owner = owner;
        }
    }

    public class ProcessStateEventArgs : EventArgs {
        public ProcessStateData Data { get; set; }

        public ProcessStateEventArgs(ProcessStateData data) {
            Data = data;
        }
    }

    public class ProjectParameterEventArgs : EventArgs {
        public ProjectParameter ProjectParameter { get; set; }

        public ProjectParameterEventArgs(ProjectParameter projectParameter) {
            ProjectParameter = projectParameter;
        }
    }
}
