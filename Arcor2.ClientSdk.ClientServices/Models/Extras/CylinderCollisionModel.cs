﻿using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    public class CylinderCollisionModel : CollisionModel {
        public decimal Radius { get; set; }
        public decimal Height { get; set; }

        public CylinderCollisionModel(decimal radius, decimal height) {
            Radius = radius;
            Height = height;
        }

        internal override ObjectModel ToOpenApiObjectModel(string id) {
            return new ObjectModel(ObjectModel.TypeEnum.Cylinder, cylinder: new Cylinder(id, Radius, Height ));
        }
    }
}