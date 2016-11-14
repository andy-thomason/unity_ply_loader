using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

public class PlyLoader {
  public PlyLoader() {
  }

  enum Format {
    ascii,
    binary_little_endian,
    binary_big_endian
  };


  Format getFormat(string s) {
    switch (s) {
      //case "ascii": return Format.ascii;
      case "binary_little_endian": return Format.binary_little_endian;
      //case "binary_big_endian": return Format.binary_big_endian;
      default: throw(new Exception("bad PLY format " + s + " (currently only binary_little_endian supported"));
    }
  }

  enum NumFormat {
    nf_uchar,
    nf_ushort,
    nf_uint,
    nf_char,
    nf_short,
    nf_int,
    nf_float,
  };

  NumFormat getNumFormat(string s) {
    switch (s) {
      case "uchar": return NumFormat.nf_uchar;
      case "ushort": return NumFormat.nf_ushort;
      case "uint": return NumFormat.nf_uint;
      case "char": return NumFormat.nf_char;
      case "short": return NumFormat.nf_short;
      case "int": return NumFormat.nf_int;
      case "float": return NumFormat.nf_float;
      default: throw(new Exception("bad PLY format " + s));
    }
  }

  public Mesh[] load(string filename) {
    if (File.Exists(filename)) {
      using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open))) {
        // Parse the ASCII text at the start up to "end_header\n"
        bool done = false;
        Format fmt = Format.ascii;
        int num_vertices = 0;
        int num_faces = 0;
        int num_vertex_props = 0;
        List<NumFormat> prop_types = new List<NumFormat>();
        List<string> prop_names = new List<string>();
        List<float []> prop_values = new List<float []>();
        string cur_element = "";
        while (!done) {
          StringBuilder builder = new StringBuilder("");
          byte b;
          for(;;) {
            b = reader.ReadByte();
            if (b == '\n') break;
            builder.Append((char)b);
          }
          string line = builder.ToString();
          string [] words = line.Split(' ');
          switch (words[0]) {
            case "ply": {
            } break;
            case "comment": {
            } break;
            case "format": {
              fmt = getFormat(words[1]);
            } break;
            case "element": {
              cur_element = words[1];
              if (words[1] == "vertex") {
                num_vertices = int.Parse(words[2]);
              } else if (words[1] == "face") {
                num_faces = int.Parse(words[2]);
              } else {
                throw(new Exception("bad PLY element"));
              }
            } break;
            case "property": {
              if (cur_element == "vertex") {
                prop_types.Add(getNumFormat(words[1]));
                prop_names.Add(words[2]);
                num_vertex_props++;
              } else if (cur_element == "face") {
                if (line != "property list uchar uint vertex_indices") {
                  throw(new Exception("bad PLY face format"));
                }
              }
            } break;
            case "end_header": {
              done = true;
            } break;
            default: {
              throw(new Exception("bad PLY field"));
            };
          }
        }

        int num_props = prop_names.Count;

        //Debug.Log("" + num_vertices + " vertices " + num_faces  + " faces " + num_props + " props!");

        for (int i = 0; i != num_props; ++i) {
          prop_values.Add(new float[num_vertices]);
        }

        int prop_x = -1;
        int prop_y = -1;
        int prop_z = -1;
        int prop_nx = -1;
        int prop_ny = -1;
        int prop_nz = -1;
        int prop_u = -1;
        int prop_v = -1;
        int prop_r = -1;
        int prop_g = -1;
        int prop_b = -1;
        int prop_a = -1;

        for (int i = 0; i != num_vertices; ++i) {
          for (int j = 0; j != num_props; ++j) {
            if (fmt == Format.binary_little_endian) {
              switch (prop_types[j]) {
                case NumFormat.nf_uchar: prop_values[j][i] = (byte)reader.ReadByte() * (1.0f/255); break;
                case NumFormat.nf_ushort:  prop_values[j][i] = (ushort)reader.ReadInt16() * (1.0f/65535); break;
                case NumFormat.nf_uint:  prop_values[j][i] = (uint)reader.ReadInt32() * (1.0f/0xffffffff); break;
                case NumFormat.nf_char: prop_values[j][i] = (sbyte)reader.ReadByte() * (1.0f/127); break; // not absolutely correct
                case NumFormat.nf_short:  prop_values[j][i] = (short)reader.ReadInt16() * (1.0f/32767); break;
                case NumFormat.nf_int:  prop_values[j][i] = reader.ReadInt32() * (1.0f/0x7fffffff); break;
                case NumFormat.nf_float:  prop_values[j][i] = reader.ReadSingle(); break;
              }
            }
            switch (prop_names[j]) {
              case "x": prop_x = j; break;
              case "y": prop_y = j; break;
              case "z": prop_z = j; break;
              case "nx": prop_nx = j; break;
              case "ny": prop_ny = j; break;
              case "nz": prop_nz = j; break;
              case "u": prop_u = j; break;
              case "v": prop_v = j; break;
              case "red": prop_r = j; break;
              case "green": prop_g = j; break;
              case "blue": prop_b = j; break;
              case "alpha": prop_a = j; break;
            }
          }
        }

        bool no_xyz = (prop_x == -1 || prop_y == -1 || prop_z == -1);
        bool no_nxyz = (prop_nx == -1 || prop_ny == -1 || prop_nz == -1);
        bool no_uv = (prop_u == -1 || prop_v == -1);
        bool no_rgb = (prop_r == -1 || prop_g == -1 || prop_b == -1);

        if (no_xyz) {
          throw(new Exception("bad PLY no xyz"));
        }

        List<Mesh> meshes = new List<Mesh>();
        Dictionary<int, int> global_to_local;

        // local values (limited to 64k vertices)
        int num_local = 0;
        List<int> indices;
        List<Vector3> vertices;
        List<Color32> colors;
        List<Vector3> normals;
        List<Vector2> uvs;

        {
          indices = new List<int>();
          global_to_local = new Dictionary<int, int>();
          vertices = new List<Vector3>();
          colors = new List<Color32>();
          normals = new List<Vector3>();
          uvs = new List<Vector2>();
          num_local = 0;
        }

        for (int i = 0; i != num_faces; ++i) {
          int nj = reader.ReadByte();
          int a = reader.ReadInt32();
          int b = reader.ReadInt32();
          for (int j = 2; j < nj; ++j) {
            int c = reader.ReadInt32();
            int [] abc = { a, b, c };
            foreach (int g in abc) {
              // translate from global to local indices (64k limit)
              if (global_to_local.ContainsKey(g)) {
                indices.Add(global_to_local[g]);
              } else {
                List<float []> pv = prop_values;
                vertices.Add(new Vector3(pv[prop_x][g], pv[prop_y][g], pv[prop_z][g]));
                if (!no_nxyz) normals.Add(new Vector3(pv[prop_nx][g], pv[prop_ny][g], pv[prop_nz][g]));
                if (!no_uv) uvs.Add(new Vector2(pv[prop_u][g], pv[prop_v][g]));
                if (!no_rgb) colors.Add(new Color32((byte)(pv[prop_r][g] * 255.0f), (byte)(pv[prop_g][g] * 255.0f), (byte)(pv[prop_b][g] * 255.0f), 255));
                global_to_local[g] = num_local;
                indices.Add(num_local++);
              }
            }
            b = c;
          }
          if (num_local >= 65530 || i+1 == num_faces) {
            Mesh mesh = new Mesh();
            meshes.Add(mesh);
            //Debug.Log("vertices=" + vertices.Count + " normals=" + normals.Count + " uvs=" + uvs.Count + " colors=" + colors.Count);
            mesh.vertices = vertices.ToArray();
            if (!no_uv) mesh.uv = uvs.ToArray();
            if (!no_nxyz) mesh.normals = normals.ToArray();
            if (!no_rgb) mesh.colors32 = colors.ToArray();
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0, true);
            mesh.name = "mesh";

            indices = new List<int>();
            global_to_local = new Dictionary<int, int>();
            vertices = new List<Vector3>();
            colors = new List<Color32>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
            num_local = 0;
          }
        }
        return meshes.ToArray();
      }
    }
    return new Mesh[0];
  }
}
