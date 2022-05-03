using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationPlugin3
{
    [TransactionAttribute(TransactionMode.Manual)]


    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            Document doc = commandData.Application.ActiveUIDocument.Document;
            Level level1, level2;
            GetLevels(doc, out level1, out level2);
            CreateWalls(doc, level1, level2);

            return Result.Succeeded;
        }

        private static void GetLevels(Document doc, out Level level1, out Level level2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level))
                            .OfType<Level>()
                            .ToList();

            level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();
            level2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();
        }

        private static void CreateWalls(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1]);
            AddWindow(doc, level1, walls[2]);
            AddWindow(doc, level1, walls[3]);
            AddExtrusionRoof(doc, level2, walls);
            transaction.Commit();
        }

        private static void AddExtrusionRoof(Document doc, Level level2, List<Wall> walls)
        {
            LocationCurve curve = walls[1].Location as LocationCurve;
            XYZ startPoint = curve.Curve.GetEndPoint(0);
            XYZ endPoint = curve.Curve.GetEndPoint(1);

            LocationCurve locationCurve = walls[0].Location as LocationCurve;
            var distance = locationCurve.Curve.Length + 4;

            RoofType roofType = new FilteredElementCollector(doc)
                        .OfClass(typeof(RoofType))
                        .OfType<RoofType>()
                        .Where(x => x.Name.Equals("Типовой - 400мм"))
                        .FirstOrDefault();

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(0, startPoint.Y - 2, level2.Elevation), new XYZ(0, 0, level2.Elevation + 10)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, level2.Elevation + 10), new XYZ(0, endPoint.Y + 2, level2.Elevation)));

            ReferencePlane refPlane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 1), new XYZ(0, 1, 0), doc.ActiveView);
            ExtrusionRoof roof = doc.Create.NewExtrusionRoof(curveArray, refPlane, level2, roofType, -distance * 0.5, distance * 0.5);
        }

        private static void AddDoor(Document doc, Level level_1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point_1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point_2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point_1 + point_2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level_1, StructuralType.NonStructural);
        }

        private static void AddWindow(Document doc, Level level_1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1220 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point_1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point_2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point_1 + point_2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();
            var window = doc.Create.NewFamilyInstance(point, windowType, wall, level_1, StructuralType.NonStructural);
            Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters);
            sillHeight.Set(sh);
        }
    }
}
