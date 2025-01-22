using System;
using System.Collections.Generic;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.Communication {
    // This file contains EventArgs definition for
    // different callbacks used in Arcor2Client

    public abstract class ParentIdEventArgs : EventArgs {
        public string ParentId { get; set; }

        public bool IsParentIdSet() {
            return !string.IsNullOrEmpty(ParentId);
        }
        protected ParentIdEventArgs(string parentId = "") {
            ParentId = parentId;
        }
    }

    public class ProjectExceptionEventArgs : EventArgs {
        public ProjectExceptionData Data { get; set; }

        public ProjectExceptionEventArgs(ProjectExceptionData data) {
            Data = data;
        }
    }

    public class HandTeachingModeEventArgs : EventArgs {
        public HandTeachingModeData Data { get; set; }

        public HandTeachingModeEventArgs(HandTeachingModeData data) {
            Data = data;
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

    public class BareActionEventArgs : ParentIdEventArgs {
        public BareAction Action { get; set; }

        public BareActionEventArgs(BareAction action, string parentId = "") : base(parentId) {
            Action = action;
        }
    }

    public class BareActionPointEventArgs : EventArgs {
        public BareActionPoint ActionPoint { get; set; }

        public BareActionPointEventArgs(BareActionPoint actionPoint) {
            ActionPoint = actionPoint;
        }
    }

    public class RobotEndEffectorUpdatedEventArgs : EventArgs {
        public RobotEefData Data { get; set; }

        public RobotEndEffectorUpdatedEventArgs(RobotEefData data) {
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

    public class OrientationEventArgs : ParentIdEventArgs {
        public NamedOrientation Data { get; set; }

        public OrientationEventArgs(NamedOrientation data, string parentId = "") : base(parentId) {
            Data = data;
        }
    }

    public class JointsEventArgs : ParentIdEventArgs {
        public ProjectRobotJoints Data { get; set; }

        public JointsEventArgs(ProjectRobotJoints data, string parentId = "") : base(parentId) {
            Data = data;
        }
    }

    public class ObjectsLockEventArgs : EventArgs {
        public LockData Data { get; set; }

        public ObjectsLockEventArgs(LockData data) {
            Data = data;
        }
    }

    public class ActionStateBeforeEventArgs : EventArgs {
        public ActionStateBeforeData Data { get; set; }

        public ActionStateBeforeEventArgs(ActionStateBeforeData data) {
            Data = data;
        }
    }

    public class ActionStateAfterEventArgs : EventArgs {
        public ActionStateAfterData Data { get; set; }

        public ActionStateAfterEventArgs(ActionStateAfterData data) {
            Data = data;
        }
    }

    public class PackageStateEventArgs : EventArgs {
        public PackageStateData Data { get; set; }

        public PackageStateEventArgs(PackageStateData data) {
            Data = data;
        }
    }

    public class PackageInfoEventArgs : EventArgs {
        public PackageInfoData Data { get; set; }

        public PackageInfoEventArgs(PackageInfoData data) {
            Data = data;
        }
    }

    public class OpenSceneEventArgs : EventArgs {
        public OpenSceneData Data { get; set; }

        public OpenSceneEventArgs(OpenSceneData data) {
            Data = data;
        }
    }

    public class OpenProjectEventArgs : EventArgs {
        public OpenProjectData Data { get; set; }

        public OpenProjectEventArgs(OpenProjectData data) {
            Data = data;
        }
    }

    public class ActionExecutionEventArgs : EventArgs {
        public ActionExecutionData Data { get; set; }

        public ActionExecutionEventArgs(ActionExecutionData data) {
            Data = data;
        }
    }

    public class ActionResultEventArgs : EventArgs {
        public ActionResultData Data { get; set; }

        public ActionResultEventArgs(ActionResultData data) {
            Data = data;
        }
    }

    public class RobotMoveToPoseEventArgs : EventArgs {
        public RobotMoveToPoseData Data { get; set; }

        public RobotMoveToPoseEventArgs(RobotMoveToPoseData data) {
            Data = data;
        }
    }

    public class RobotMoveToJointsEventArgs : EventArgs {

        public RobotMoveToJointsData Data { get; set; }

        public RobotMoveToJointsEventArgs(RobotMoveToJointsData data) {
            Data = data;
        }
    }

    public class RobotMoveToActionPointJointsEventArgs : EventArgs {

        public RobotMoveToActionPointJointsData Data { get; set; }

        public RobotMoveToActionPointJointsEventArgs(RobotMoveToActionPointJointsData data) {
            Data = data;
        }
    }

    public class RobotMoveToActionPointOrientationEventArgs : EventArgs {

        public RobotMoveToActionPointOrientationData Data { get; set; }

        public RobotMoveToActionPointOrientationEventArgs(RobotMoveToActionPointOrientationData data) {
            Data = data;
        }
    }

    public class SceneStateEventArgs : EventArgs {
        public SceneStateData Data { get; set; }

        public SceneStateEventArgs(SceneStateData data) {
            Data = data;
        }
    }

    public class ParameterEventArgs : ParentIdEventArgs {

        public Parameter Parameter { get; set; }

        public ParameterEventArgs(Parameter parameter, string parentId = "") : base(parentId) {
            Parameter = parameter;
        }
    }

    public class ObjectTypesEventArgs : EventArgs {
        public List<ObjectTypeMeta> ObjectTypes { get; set; }

        public ObjectTypesEventArgs(List<ObjectTypeMeta> objectTypes) {
            ObjectTypes = objectTypes;
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

    public class SceneActionObjectEventArgs : EventArgs {
        public SceneObject SceneObject { get; set; }

        public SceneActionObjectEventArgs(SceneObject sceneObject) {
            SceneObject = sceneObject;
        }
    }

    public class PackageChangedEventArgs : EventArgs {
        public PackageSummary Data { get; set; }

        public PackageChangedEventArgs(PackageSummary data) {
            Data = data;
        }
    }
}
