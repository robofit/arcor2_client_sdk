using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.UnitTests;

public class CollisionModelTests {
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 2, 3)]
    [InlineData(-4, -6, -10)]
    [InlineData(-2, 2, 0)]
    [InlineData(int.MinValue, 2, int.MaxValue)]
    public void BoxCollisionModel_Valid_CorrectRepresentation(decimal x, decimal y, decimal z) {
        var box = new BoxCollisionModel(x, y, z);
        Assert.Equal(x, box.X);
        Assert.Equal(y, box.Y);
        Assert.Equal(z, box.Z);

        var model = box.ToObjectModel("TestID");
        Assert.Null(model.Cylinder);
        Assert.Null(model.Mesh);
        Assert.Null(model.Sphere);
        Assert.NotNull(model.Box);
        Assert.Equal(ObjectModel.TypeEnum.Box, model.Type);
        Assert.Equal(x, model.Box.SizeX);
        Assert.Equal(y, model.Box.SizeY);
        Assert.Equal(z, model.Box.SizeZ);
        Assert.Equal("TestID", model.Box.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-4)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void SphereCollisionModel_Valid_CorrectRepresentation(decimal r) {
        var sphere = new SphereCollisionModel(r);
        Assert.Equal(r, sphere.Radius);

        var model = sphere.ToObjectModel("TestID");
        Assert.Null(model.Cylinder);
        Assert.Null(model.Mesh);
        Assert.NotNull(model.Sphere);
        Assert.Null(model.Box);
        Assert.Equal(ObjectModel.TypeEnum.Sphere, model.Type);
        Assert.Equal(r, model.Sphere.Radius);
        Assert.Equal("TestID", model.Sphere.Id);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 20)]
    [InlineData(-4, -78)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(int.MinValue, int.MinValue)]
    public void CylinderCollisionModel_Valid_CorrectRepresentation(decimal r, decimal h) {
        var cylinder = new CylinderCollisionModel(r, h);
        Assert.Equal(r, cylinder.Radius);
        Assert.Equal(h, cylinder.Height);

        var model = cylinder.ToObjectModel("TestID");
        Assert.NotNull(model.Cylinder);
        Assert.Null(model.Mesh);
        Assert.Null(model.Sphere);
        Assert.Null(model.Box);
        Assert.Equal(ObjectModel.TypeEnum.Cylinder, model.Type);
        Assert.Equal(r, model.Cylinder.Radius);
        Assert.Equal(h, model.Cylinder.Height);
        Assert.Equal("TestID", model.Cylinder.Id);
    }

    [Fact]
    public void MeshCollisionModel_Valid_CorrectRepresentation() {
        var mesh = new MeshCollisionModel("AssetID", [
            new Pose(new Position()),
            new Pose(new Position(1)),
            new Pose(new Position(0, 1))
        ]);
        Assert.Equal(3, mesh.Points.Count);

        var model = mesh.ToObjectModel("TestID");
        Assert.Null(model.Cylinder);
        Assert.NotNull(model.Mesh);
        Assert.Null(model.Sphere);
        Assert.Null(model.Box);
        Assert.Equal(ObjectModel.TypeEnum.Mesh, model.Type);
        Assert.Equal("AssetID", model.Mesh.AssetId);
        Assert.Equal(3, model.Mesh.FocusPoints.Count);
        Assert.Equal("TestID", model.Mesh.Id);
    }
}