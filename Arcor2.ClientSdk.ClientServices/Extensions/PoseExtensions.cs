using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class PoseExtensions {
        public static bool EqualTo(this Pose pose, Pose otherPose, decimal epsilon = 0) {
            return pose.Orientation.EqualTo(otherPose.Orientation) && pose.Position.EqualTo(otherPose.Position);
        }

        public static bool EqualTo(this Orientation orientation, Orientation otherOrientation, decimal epsilon = 0) {
            return Math.Abs(orientation.X - otherOrientation.X) < epsilon &&
                   Math.Abs(orientation.Y - otherOrientation.Y) < epsilon &&
                   Math.Abs(orientation.Z - otherOrientation.Z) < epsilon &&
                   Math.Abs(orientation.W - otherOrientation.W) < epsilon;
        }

        public static bool EqualTo(this Position position, Position otherPosition, decimal epsilon = 0) {
            return Math.Abs(position.X - otherPosition.X) < epsilon &&
                   Math.Abs(position.Y - otherPosition.Y) < epsilon &&
                   Math.Abs(position.Z - otherPosition.Z) < epsilon;
        }
    }
}