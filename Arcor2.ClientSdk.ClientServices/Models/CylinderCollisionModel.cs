using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    public class CylinderCollisionModel : CollisionModel
    {
        public decimal Radius { get; set; }
        public decimal Height { get; set; }

        public CylinderCollisionModel(decimal radius, decimal height)
        {
            Radius = radius;
            Height = height;
        }

        public override ObjectModel ToObjectModel(string id)
        {
            return new ObjectModel(ObjectModel.TypeEnum.Cylinder, cylinder: new Cylinder(id, Radius, Height));
        }
    }
}