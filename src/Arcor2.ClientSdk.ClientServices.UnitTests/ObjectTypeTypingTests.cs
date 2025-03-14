using Arcor2.ClientSdk.ClientServices.Managers;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.UnitTests;

public class ObjectTypeTypingTests {
    private ObjectTypeManager Camera;
    private ObjectTypeManager CollisionObject;
    private ObjectTypeManager Cube;
    private ObjectTypeManager DobotM1;
    private ObjectTypeManager DobotM2;
    private ObjectTypeManager Generic;
    private ObjectTypeManager GenericWithColor;
    private ObjectTypeManager GenericWithPose;
    private ObjectTypeManager MultiArmRobot;
    private ObjectTypeManager MultiArmTerminator;
    private ObjectTypeManager Nikon;
    private ObjectTypeManager Robot;
    private ObjectTypeManager VirtualCollisionObject;

    public ObjectTypeTypingTests() {
        // Create a session with mock type system
        // We just want to test the client method 
        var mockSession = new Arcor2Session();
        Generic = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "Generic",
            varBase: "",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(Generic);
        GenericWithPose = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "GenericWithPose",
            varBase: "Generic",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(GenericWithPose);
        CollisionObject = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "CollisionObject",
            varBase: "GenericWithPose",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(CollisionObject);
        VirtualCollisionObject = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "VirtualCollisionObject",
            varBase: "CollisionObject",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(VirtualCollisionObject);
        Camera = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "Camera",
            varBase: "CollisionObject",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(Camera);
        Robot = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "Robot",
            varBase: "GenericWithPose",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(Robot);
        MultiArmRobot = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "MultiArmRobot",
            varBase: "Robot",
            varAbstract: true,
            builtIn: true));
        mockSession.objectTypes.Add(MultiArmRobot);
        DobotM1 = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "DobotM1",
            varBase: "Robot",
            varAbstract: false,
            builtIn: false));
        mockSession.objectTypes.Add(DobotM1);
        DobotM2 = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "DobotM2",
            varBase: "Robot",
            varAbstract: false,
            builtIn: false));
        mockSession.objectTypes.Add(DobotM2);
        MultiArmTerminator = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "MultiArmTerminator",
            varBase: "MultiArmRobot",
            varAbstract: false,
            builtIn: false));
        mockSession.objectTypes.Add(MultiArmTerminator);
        Nikon = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "Nikon",
            varBase: "Camera",
            varAbstract: false,
            builtIn: false));
        mockSession.objectTypes.Add(Nikon);
        GenericWithColor = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "GenericWithColor",
            varBase: "Generic",
            needsParentType: "GenericWithPose",
            varAbstract: true,
            builtIn: false));
        mockSession.objectTypes.Add(GenericWithColor);
        Cube = new ObjectTypeManager(mockSession, new ObjectTypeMeta(
            "Cube",
            varBase: "VirtualCollisionObject",
            varAbstract: false,
            builtIn: false));
        mockSession.objectTypes.Add(Cube);
    }

    [Fact]
    public void IsTypeOf_Generic_True() {
        Assert.True(Generic.IsTypeOf(Generic));
        Assert.True(GenericWithPose.IsTypeOf(Generic));
        Assert.True(CollisionObject.IsTypeOf(Generic));
        Assert.True(VirtualCollisionObject.IsTypeOf(Generic));
        Assert.True(Camera.IsTypeOf(Generic));
        Assert.True(Robot.IsTypeOf(Generic));
        Assert.True(MultiArmRobot.IsTypeOf(Generic));
        Assert.True(DobotM1.IsTypeOf(Generic));
        Assert.True(DobotM2.IsTypeOf(Generic));
        Assert.True(MultiArmTerminator.IsTypeOf(Generic));
        Assert.True(Nikon.IsTypeOf(Generic));
        Assert.True(GenericWithColor.IsTypeOf(Generic));
        Assert.True(Cube.IsTypeOf(Generic));
    }

    [Fact]
    public void IsTypeOf_EmptyString_False() {
        Assert.False(Generic.IsTypeOf(""));
        Assert.False(GenericWithPose.IsTypeOf(""));
        Assert.False(CollisionObject.IsTypeOf(""));
        Assert.False(VirtualCollisionObject.IsTypeOf(""));
        Assert.False(Camera.IsTypeOf(""));
        Assert.False(Robot.IsTypeOf(""));
        Assert.False(MultiArmRobot.IsTypeOf(""));
        Assert.False(DobotM1.IsTypeOf(""));
        Assert.False(DobotM2.IsTypeOf(""));
        Assert.False(MultiArmTerminator.IsTypeOf(""));
        Assert.False(Nikon.IsTypeOf(""));
        Assert.False(GenericWithColor.IsTypeOf(""));
        Assert.False(Cube.IsTypeOf(""));
    }

    [Fact]
    public void IsTypeOf_Itself_True() {
        Assert.True(Generic.IsTypeOf(Generic));
        Assert.True(GenericWithPose.IsTypeOf(GenericWithPose));
        Assert.True(CollisionObject.IsTypeOf(CollisionObject));
        Assert.True(VirtualCollisionObject.IsTypeOf(VirtualCollisionObject));
        Assert.True(Camera.IsTypeOf(Camera));
        Assert.True(Robot.IsTypeOf(Robot));
        Assert.True(MultiArmRobot.IsTypeOf(MultiArmRobot));
        Assert.True(DobotM1.IsTypeOf(DobotM1));
        Assert.True(DobotM2.IsTypeOf(DobotM2));
        Assert.True(MultiArmTerminator.IsTypeOf(MultiArmTerminator));
        Assert.True(Nikon.IsTypeOf(Nikon));
        Assert.True(GenericWithColor.IsTypeOf(GenericWithColor));
        Assert.True(Cube.IsTypeOf(Cube));
    }

    [Fact]
    public void IsSubtypeOf_Itself_False() {
        Assert.False(Generic.IsSubtypeOf(Generic));
        Assert.False(GenericWithPose.IsSubtypeOf(GenericWithPose));
        Assert.False(CollisionObject.IsSubtypeOf(CollisionObject));
        Assert.False(VirtualCollisionObject.IsSubtypeOf(VirtualCollisionObject));
        Assert.False(Camera.IsSubtypeOf(Camera));
        Assert.False(Robot.IsSubtypeOf(Robot));
        Assert.False(MultiArmRobot.IsSubtypeOf(MultiArmRobot));
        Assert.False(DobotM1.IsSubtypeOf(DobotM1));
        Assert.False(DobotM2.IsSubtypeOf(DobotM2));
        Assert.False(MultiArmTerminator.IsSubtypeOf(MultiArmTerminator));
        Assert.False(Nikon.IsSubtypeOf(Nikon));
        Assert.False(GenericWithColor.IsSubtypeOf(GenericWithColor));
        Assert.False(Cube.IsSubtypeOf(Cube));
    }

    [Fact]
    public void IsTypeOf_Parent_True() {
        Assert.True(GenericWithPose.IsTypeOf(Generic));
        Assert.True(CollisionObject.IsTypeOf(GenericWithPose));
        Assert.True(VirtualCollisionObject.IsTypeOf(CollisionObject));
        Assert.True(Camera.IsTypeOf(CollisionObject));
        Assert.True(Robot.IsTypeOf(GenericWithPose));
        Assert.True(MultiArmRobot.IsTypeOf(Robot));
        Assert.True(DobotM1.IsTypeOf(Robot));
        Assert.True(DobotM2.IsTypeOf(Robot));
        Assert.True(MultiArmTerminator.IsTypeOf(MultiArmRobot));
        Assert.True(Nikon.IsTypeOf(Camera));
        Assert.True(GenericWithColor.IsTypeOf(Generic));
        Assert.True(Cube.IsTypeOf(VirtualCollisionObject));
    }

    [Fact]
    public void IsSubtypeOf_Parent_True() {
        Assert.True(GenericWithPose.IsSubtypeOf(Generic));
        Assert.True(CollisionObject.IsSubtypeOf(GenericWithPose));
        Assert.True(VirtualCollisionObject.IsSubtypeOf(CollisionObject));
        Assert.True(Camera.IsSubtypeOf(CollisionObject));
        Assert.True(Robot.IsSubtypeOf(GenericWithPose));
        Assert.True(MultiArmRobot.IsSubtypeOf(Robot));
        Assert.True(DobotM1.IsSubtypeOf(Robot));
        Assert.True(DobotM2.IsSubtypeOf(Robot));
        Assert.True(MultiArmTerminator.IsSubtypeOf(MultiArmRobot));
        Assert.True(Nikon.IsSubtypeOf(Camera));
        Assert.True(GenericWithColor.IsSubtypeOf(Generic));
        Assert.True(Cube.IsSubtypeOf(VirtualCollisionObject));
    }

    [Fact]
    public void IsTypeOf_NonAncestor_False() {
        Assert.False(GenericWithPose.IsTypeOf(Robot));
        Assert.False(CollisionObject.IsTypeOf(VirtualCollisionObject));
        Assert.False(VirtualCollisionObject.IsTypeOf(Camera));
        Assert.False(Camera.IsTypeOf(DobotM1));
        Assert.False(Robot.IsTypeOf(MultiArmRobot));
        Assert.False(MultiArmRobot.IsTypeOf(Camera));
        Assert.False(DobotM1.IsTypeOf(Nikon));
        Assert.False(DobotM2.IsTypeOf(DobotM1));
        Assert.False(MultiArmTerminator.IsTypeOf(DobotM1));
        Assert.False(Nikon.IsTypeOf(MultiArmTerminator));
        Assert.False(GenericWithColor.IsTypeOf(GenericWithPose));
        Assert.False(Cube.IsTypeOf(Robot));
    }

    [Fact]
    public void IsSubtypeOf_NonAncestor_False() {
        Assert.False(GenericWithPose.IsSubtypeOf(Robot));
        Assert.False(CollisionObject.IsSubtypeOf(VirtualCollisionObject));
        Assert.False(VirtualCollisionObject.IsSubtypeOf(Camera));
        Assert.False(Camera.IsSubtypeOf(DobotM1));
        Assert.False(Robot.IsSubtypeOf(MultiArmRobot));
        Assert.False(MultiArmRobot.IsSubtypeOf(Camera));
        Assert.False(DobotM1.IsSubtypeOf(Nikon));
        Assert.False(DobotM2.IsSubtypeOf(DobotM1));
        Assert.False(MultiArmTerminator.IsSubtypeOf(DobotM1));
        Assert.False(Nikon.IsSubtypeOf(MultiArmTerminator));
        Assert.False(GenericWithColor.IsSubtypeOf(GenericWithPose));
        Assert.False(Cube.IsSubtypeOf(Robot));
    }

    [Fact]
    public void IsTypeOf_RandomAncestor_True() {
        Assert.True(VirtualCollisionObject.IsTypeOf(GenericWithPose));
        Assert.True(Camera.IsTypeOf(GenericWithPose));
        Assert.True(Robot.IsTypeOf(GenericWithPose));
        Assert.True(MultiArmRobot.IsTypeOf(GenericWithPose));
        Assert.True(DobotM1.IsTypeOf(GenericWithPose));
        Assert.True(DobotM2.IsTypeOf(GenericWithPose));
        Assert.True(MultiArmTerminator.IsTypeOf(Robot));
        Assert.True(Nikon.IsTypeOf(CollisionObject));
        Assert.True(GenericWithColor.IsTypeOf(Generic));
        Assert.True(Cube.IsTypeOf(CollisionObject));
    }

    [Fact]
    public void IsSubtypeOf_RandomAncestor_True() {
        Assert.True(VirtualCollisionObject.IsSubtypeOf(GenericWithPose));
        Assert.True(Camera.IsSubtypeOf(GenericWithPose));
        Assert.True(Robot.IsSubtypeOf(GenericWithPose));
        Assert.True(MultiArmRobot.IsSubtypeOf(GenericWithPose));
        Assert.True(DobotM1.IsSubtypeOf(GenericWithPose));
        Assert.True(DobotM2.IsSubtypeOf(GenericWithPose));
        Assert.True(MultiArmTerminator.IsSubtypeOf(Robot));
        Assert.True(Nikon.IsSubtypeOf(CollisionObject));
        Assert.True(GenericWithColor.IsSubtypeOf(Generic));
        Assert.True(Cube.IsSubtypeOf(CollisionObject));
    }

    [Fact]
    public void IsGenericWithPose_Valid_Correct() {
        Assert.False(Generic.IsGenericWithPose());
        Assert.True(GenericWithPose.IsGenericWithPose());
        Assert.True(CollisionObject.IsGenericWithPose());
        Assert.True(VirtualCollisionObject.IsGenericWithPose());
        Assert.True(Camera.IsGenericWithPose());
        Assert.True(Robot.IsGenericWithPose());
        Assert.True(MultiArmRobot.IsGenericWithPose());
        Assert.True(DobotM1.IsGenericWithPose());
        Assert.True(DobotM2.IsGenericWithPose());
        Assert.True(MultiArmTerminator.IsGenericWithPose());
        Assert.True(Nikon.IsGenericWithPose());
        Assert.False(GenericWithColor.IsGenericWithPose());
        Assert.True(Cube.IsGenericWithPose());
    }

    [Fact]
    public void IsCollisionObject_Valid_Correct() {
        Assert.False(Generic.IsCollisionObject());
        Assert.False(GenericWithPose.IsCollisionObject());
        Assert.True(CollisionObject.IsCollisionObject());
        Assert.True(VirtualCollisionObject.IsCollisionObject());
        Assert.True(Camera.IsCollisionObject());
        Assert.False(Robot.IsCollisionObject());
        Assert.False(MultiArmRobot.IsCollisionObject());
        Assert.False(DobotM1.IsCollisionObject());
        Assert.False(DobotM2.IsCollisionObject());
        Assert.False(MultiArmTerminator.IsCollisionObject());
        Assert.True(Nikon.IsCollisionObject());
        Assert.False(GenericWithColor.IsCollisionObject());
        Assert.True(Cube.IsCollisionObject());
    }

    [Fact]
    public void IsVirtualCollisionObject_Valid_Correct() {
        Assert.False(Generic.IsVirtualCollisionObject());
        Assert.False(GenericWithPose.IsVirtualCollisionObject());
        Assert.False(CollisionObject.IsVirtualCollisionObject());
        Assert.True(VirtualCollisionObject.IsVirtualCollisionObject());
        Assert.False(Camera.IsVirtualCollisionObject());
        Assert.False(Robot.IsVirtualCollisionObject());
        Assert.False(MultiArmRobot.IsVirtualCollisionObject());
        Assert.False(DobotM1.IsVirtualCollisionObject());
        Assert.False(DobotM2.IsVirtualCollisionObject());
        Assert.False(MultiArmTerminator.IsVirtualCollisionObject());
        Assert.False(Nikon.IsVirtualCollisionObject());
        Assert.False(GenericWithColor.IsVirtualCollisionObject());
        Assert.True(Cube.IsVirtualCollisionObject());
    }

    [Fact]
    public void IsRobot_Valid_Correct() {
        Assert.False(Generic.IsRobot());
        Assert.False(GenericWithPose.IsRobot());
        Assert.False(CollisionObject.IsRobot());
        Assert.False(VirtualCollisionObject.IsRobot());
        Assert.False(Camera.IsRobot());
        Assert.True(Robot.IsRobot());
        Assert.True(MultiArmRobot.IsRobot());
        Assert.True(DobotM1.IsRobot());
        Assert.True(DobotM2.IsRobot());
        Assert.True(MultiArmTerminator.IsRobot());
        Assert.False(Nikon.IsRobot());
        Assert.False(GenericWithColor.IsRobot());
        Assert.False(Cube.IsRobot());
    }

    [Fact]
    public void IsMultiArmRobot_Valid_Correct() {
        Assert.False(Generic.IsMultiArmRobot());
        Assert.False(GenericWithPose.IsMultiArmRobot());
        Assert.False(CollisionObject.IsMultiArmRobot());
        Assert.False(VirtualCollisionObject.IsMultiArmRobot());
        Assert.False(Camera.IsMultiArmRobot());
        Assert.False(Robot.IsMultiArmRobot());
        Assert.True(MultiArmRobot.IsMultiArmRobot());
        Assert.False(DobotM1.IsMultiArmRobot());
        Assert.False(DobotM2.IsMultiArmRobot());
        Assert.True(MultiArmTerminator.IsMultiArmRobot());
        Assert.False(Nikon.IsMultiArmRobot());
        Assert.False(GenericWithColor.IsMultiArmRobot());
        Assert.False(Cube.IsMultiArmRobot());
    }

    [Fact]
    public void IsCamera_Valid_Correct() {
        Assert.False(Generic.IsCamera());
        Assert.False(GenericWithPose.IsCamera());
        Assert.False(CollisionObject.IsCamera());
        Assert.False(VirtualCollisionObject.IsCamera());
        Assert.True(Camera.IsCamera());
        Assert.False(Robot.IsCamera());
        Assert.False(MultiArmRobot.IsCamera());
        Assert.False(DobotM1.IsCamera());
        Assert.False(DobotM2.IsCamera());
        Assert.False(MultiArmTerminator.IsCamera());
        Assert.True(Nikon.IsCamera());
        Assert.False(GenericWithColor.IsCamera());
        Assert.False(Cube.IsCamera());
    }

    [Fact]
    public void Parent_Valid_CorrespondsToDefinition() {
        Assert.Null(Generic.Parent);
        Assert.Equal(Generic, GenericWithPose.Parent);
        Assert.Equal(GenericWithPose, CollisionObject.Parent);
        Assert.Equal(CollisionObject, VirtualCollisionObject.Parent);
        Assert.Equal(CollisionObject, Camera.Parent);
        Assert.Equal(GenericWithPose, Robot.Parent);
        Assert.Equal(Robot, MultiArmRobot.Parent);
        Assert.Equal(Robot, DobotM1.Parent);
        Assert.Equal(Robot, DobotM2.Parent);
        Assert.Equal(MultiArmRobot, MultiArmTerminator.Parent);
        Assert.Equal(Camera, Nikon.Parent);
        Assert.Equal(Generic, GenericWithColor.Parent);
        Assert.Equal(VirtualCollisionObject, Cube.Parent);
    }

    [Fact]
    public void SceneParent_Valid_CorrespondsToDefinition() {
        Assert.Null(Generic.SceneParent);
        Assert.Null(GenericWithPose.SceneParent);
        Assert.Null(CollisionObject.SceneParent);
        Assert.Null(VirtualCollisionObject.SceneParent);
        Assert.Null(Camera.SceneParent);
        Assert.Null(Robot.SceneParent);
        Assert.Null(MultiArmRobot.SceneParent);
        Assert.Null(DobotM1.SceneParent);
        Assert.Null(DobotM2.SceneParent);
        Assert.Null(MultiArmTerminator.SceneParent);
        Assert.Null(Nikon.SceneParent);
        Assert.Equal(GenericWithPose, GenericWithColor.SceneParent);
        Assert.Null(Cube.SceneParent);
    }
}