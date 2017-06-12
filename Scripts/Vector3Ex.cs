using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRSketch2
{
    public abstract class Subspace
    {
        public abstract Subspace JoinedWithVector(Vector3 v);
        public abstract Subspace NormalSubspace();
        public abstract Vector3 Project(Vector3 v);

        public Subspace IntersectedWithPlane(Vector3 normal)
        {
            return NormalSubspace().JoinedWithVector(normal).NormalSubspace();
        }

        public Subspace IntersectedWithSingleVector(Vector3 v)
        {
            v = Project(v);
            return new Subspace0().JoinedWithVector(v);
        }
    }

    public class Subspace0 : Subspace
    {
        public Subspace0() { }

        public override Subspace JoinedWithVector(Vector3 v)
        {
            if (v == Vector3.zero)
                return this;
            return new Subspace1(v);
        }

        public override Subspace NormalSubspace()
        {
            return new Subspace3();
        }

        public override Vector3 Project(Vector3 v)
        {
            return Vector3.zero;
        }
    }

    public class Subspace1 : Subspace
    {
        public readonly Vector3 axis;

        public Subspace1(Vector3 axis) { this.axis = axis; }

        public override Subspace JoinedWithVector(Vector3 v)
        {
            Vector3 normal = Vector3.Cross(axis, v);
            if (normal == Vector3.zero)
                return this;
            return new Subspace2(normal);
        }

        public override Subspace NormalSubspace()
        {
            return new Subspace2(axis);
        }

        public override Vector3 Project(Vector3 v)
        {
            return Vector3.Project(v, axis);
        }
    }

    public class Subspace2 : Subspace
    {
        public readonly Vector3 normal;

        public Subspace2(Vector3 normal) { this.normal = normal; }

        public override Subspace JoinedWithVector(Vector3 v)
        {
            if (Vector3.Dot(v, normal) == 0)
                return this;
            return new Subspace3();
        }

        public override Subspace NormalSubspace()
        {
            return new Subspace1(normal);
        }

        public override Vector3 Project(Vector3 v)
        {
            return Vector3.ProjectOnPlane(v, normal);
        }
    }

    public class Subspace3 : Subspace
    {
        public Subspace3() { }

        public override Subspace JoinedWithVector(Vector3 v)
        {
            return this;
        }

        public override Subspace NormalSubspace()
        {
            return new Subspace0();
        }

        public override Vector3 Project(Vector3 v)
        {
            return v;
        }
    }


    public static class PlaneRecomputer
    {
        public static Plane RecomputePlane(List<Vertex> vertices)
        {
            Vector3 center = Vector3.zero;
            foreach (var v in vertices)
                center += v.position;
            center /= vertices.Count;

            var A = new DotNetMatrix.GeneralMatrix(3, 3);
            double[] r = new double[3];

            foreach (var v in vertices)
            {
                Vector3 p = v.position - center;
                r[0] = p.x;
                r[1] = p.y;
                r[2] = p.z;
                for (int j = 0; j < 3; j++)
                    for (int i = j; i < 3; i++)
                        A.Array[j][i] += r[i] * r[j];
            }
            for (int j = 1; j < 3; j++)
                for (int i = 0; i < j; i++)
                    A.Array[j][i] = A.Array[i][j];

            var E = A.Eigen();
            var minimal_eigenvalue = E.RealEigenvalues[0];
            int pick = 0;
            for (int i = 1; i < 3; i++)
            {
                if (E.RealEigenvalues[i] < minimal_eigenvalue)
                {
                    minimal_eigenvalue = E.RealEigenvalues[i];
                    pick = i;
                }
            }
            var eigenvectors = E.GetV();
            var normal = new Vector3((float)eigenvectors.Array[0][pick],
                                     (float)eigenvectors.Array[1][pick],
                                     (float)eigenvectors.Array[2][pick]);

            if (normal.y < -0.9f)
            {
                normal.y = normal.y;
            }
            return new Plane(normal, center);
        }
    }
}
