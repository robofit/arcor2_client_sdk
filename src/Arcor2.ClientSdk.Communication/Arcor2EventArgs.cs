using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Collections.Generic;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.Communication {
    // This file contains EventArgs definition for
    // different callbacks used in Arcor2Client

    public abstract class ParentIdEventArgs : EventArgs {
        protected ParentIdEventArgs(string parentId = "") {
            ParentId = parentId;
        }

        public string ParentId { get; set; }

        public bool IsParentIdSet() => !string.IsNullOrEmpty(ParentId);
    }

    public class PackageExceptionEventArgs : EventArgs {
        public PackageExceptionEventArgs(ProjectExceptionData data) {
            Data = data;
        }

        public ProjectExceptionData Data { get; set; }
    }

    public class HandTeachingModeEventArgs : EventArgs {
        public HandTeachingModeEventArgs(HandTeachingModeData data) {
            Data = data;
        }

        public HandTeachingModeData Data { get; set; }
    }

    public class BareProjectEventArgs : EventArgs {
        public BareProjectEventArgs(BareProject project) {
            Data = project;
        }

        public BareProject Data { get; set; }
    }

    public class BareSceneEventArgs : EventArgs {
        public BareSceneEventArgs(BareScene scene) {
            Data = scene;
        }

        public BareScene Data { get; set; }
    }

    public class BareActionEventArgs : ParentIdEventArgs {
        public BareActionEventArgs(BareAction action, string parentId = "") : base(parentId) {
            Data = action;
        }

        public BareAction Data { get; set; }
    }

    public class ActionEventArgs : ParentIdEventArgs {
        public ActionEventArgs(Action action, string parentId = "") : base(parentId) {
            Data = action;
        }

        public Action Data { get; set; }
    }

    public class BareActionPointEventArgs : EventArgs {
        public BareActionPointEventArgs(BareActionPoint actionPoint) {
            Data = actionPoint;
        }

        public BareActionPoint Data { get; set; }
    }

    public class RobotEndEffectorUpdatedEventArgs : EventArgs {
        public RobotEndEffectorUpdatedEventArgs(RobotEefData data) {
            Data = data;
        }

        public RobotEefData Data { get; set; }
    }

    public class RobotJointsUpdatedEventArgs : EventArgs {
        public RobotJointsUpdatedEventArgs(RobotJointsData data) {
            Data = data;
        }

        public RobotJointsData Data { get; set; }
    }

    public class LogicItemEventArgs : EventArgs {
        public LogicItemEventArgs(LogicItem data) {
            Data = data;
        }

        public LogicItem Data { get; set; }
    }

    public class ShowMainScreenEventArgs : EventArgs {
        public ShowMainScreenEventArgs(ShowMainScreenData data) {
            Data = data;
        }

        public ShowMainScreenData Data { get; set; }
    }

    public class OrientationEventArgs : ParentIdEventArgs {
        public OrientationEventArgs(NamedOrientation data, string parentId = "") : base(parentId) {
            Data = data;
        }

        public NamedOrientation Data { get; set; }
    }

    public class JointsEventArgs : ParentIdEventArgs {
        public JointsEventArgs(ProjectRobotJoints data, string parentId = "") : base(parentId) {
            Data = data;
        }

        public ProjectRobotJoints Data { get; set; }
    }

    public class ObjectsLockEventArgs : EventArgs {
        public ObjectsLockEventArgs(LockData data) {
            Data = data;
        }

        public LockData Data { get; set; }
    }

    public class ActionStateBeforeEventArgs : EventArgs {
        public ActionStateBeforeEventArgs(ActionStateBeforeData data) {
            Data = data;
        }

        public ActionStateBeforeData Data { get; set; }
    }

    public class ActionStateAfterEventArgs : EventArgs {
        public ActionStateAfterEventArgs(ActionStateAfterData data) {
            Data = data;
        }

        public ActionStateAfterData Data { get; set; }
    }

    public class PackageStateEventArgs : EventArgs {
        public PackageStateEventArgs(PackageStateData data) {
            Data = data;
        }

        public PackageStateData Data { get; set; }
    }

    public class PackageInfoEventArgs : EventArgs {
        public PackageInfoEventArgs(PackageInfoData data) {
            Data = data;
        }

        public PackageInfoData Data { get; set; }
    }

    public class OpenSceneEventArgs : EventArgs {
        public OpenSceneEventArgs(OpenSceneData data) {
            Data = data;
        }

        public OpenSceneData Data { get; set; }
    }

    public class OpenProjectEventArgs : EventArgs {
        public OpenProjectEventArgs(OpenProjectData data) {
            Data = data;
        }

        public OpenProjectData Data { get; set; }
    }

    public class ActionExecutionEventArgs : EventArgs {
        public ActionExecutionEventArgs(ActionExecutionData data) {
            Data = data;
        }

        public ActionExecutionData Data { get; set; }
    }

    public class ActionResultEventArgs : EventArgs {
        public ActionResultEventArgs(ActionResultData data) {
            Data = data;
        }

        public ActionResultData Data { get; set; }
    }

    public class RobotMoveToPoseEventArgs : EventArgs {
        public RobotMoveToPoseEventArgs(RobotMoveToPoseData data) {
            Data = data;
        }

        public RobotMoveToPoseData Data { get; set; }
    }

    public class RobotMoveToJointsEventArgs : EventArgs {
        public RobotMoveToJointsEventArgs(RobotMoveToJointsData data) {
            Data = data;
        }

        public RobotMoveToJointsData Data { get; set; }
    }

    public class RobotMoveToActionPointJointsEventArgs : EventArgs {
        public RobotMoveToActionPointJointsEventArgs(RobotMoveToActionPointJointsData data) {
            Data = data;
        }

        public RobotMoveToActionPointJointsData Data { get; set; }
    }

    public class RobotMoveToActionPointOrientationEventArgs : EventArgs {
        public RobotMoveToActionPointOrientationEventArgs(RobotMoveToActionPointOrientationData data) {
            Data = data;
        }

        public RobotMoveToActionPointOrientationData Data { get; set; }
    }

    public class SceneStateEventArgs : EventArgs {
        public SceneStateEventArgs(SceneStateData data) {
            Data = data;
        }

        public SceneStateData Data { get; set; }
    }

    public class ParameterEventArgs : ParentIdEventArgs {
        public ParameterEventArgs(Parameter parameter, string parentId = "") : base(parentId) {
            Data = parameter;
        }

        public Parameter Data { get; set; }
    }

    public class ObjectTypesEventArgs : EventArgs {
        public ObjectTypesEventArgs(List<ObjectTypeMeta> data) {
            Data = data;
        }

        public List<ObjectTypeMeta> Data { get; set; }
    }

    public class ProcessStateEventArgs : EventArgs {
        public ProcessStateEventArgs(ProcessStateData processState) {
            Data = processState;
        }

        public ProcessStateData Data { get; set; }
    }

    public class ProjectParameterEventArgs : EventArgs {
        public ProjectParameterEventArgs(ProjectParameter parameter) {
            Data = parameter;
        }

        public ProjectParameter Data { get; set; }
    }

    public class ActionObjectEventArgs : EventArgs {
        public ActionObjectEventArgs(SceneObject sceneObject) {
            Data = sceneObject;
        }

        public SceneObject Data { get; set; }
    }

    public class PackageEventArgs : EventArgs {
        public PackageEventArgs(PackageSummary data) {
            Data = data;
        }

        public PackageSummary Data { get; set; }
    }
}