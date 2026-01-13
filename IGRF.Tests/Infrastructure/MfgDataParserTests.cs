#nullable enable

using IGRF_Interface.Infrastructure.Utilities;

namespace IGRF.Tests.Infrastructure;

/// <summary>
/// Unit tests for MfgDataParser class
/// </summary>
public class MfgDataParserTests
{
    [Fact]
    public void Parse_ValidPacket_ReturnsStruct()
    {
        // Arrange - Create a valid 72-byte packet
        var packet = CreateValidMagDataPacket(
            dataType: MfgDataParser.TYPE_DAT,
            sensor1Mag: new[] { 50000f, 1000f, -2000f },
            sensor2Mag: new[] { 49500f, 1100f, -1900f }
        );

        // Act
        var result = MfgDataParser.Parse(packet);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MfgDataParser.TYPE_DAT, result.Value.DataType);
    }

    [Fact]
    public void Parse_NullPacket_ReturnsNull()
    {
        // Act
        var result = MfgDataParser.Parse(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_ShortPacket_ReturnsNull()
    {
        // Arrange - Packet shorter than required 72 bytes
        var packet = new byte[50];

        // Act
        var result = MfgDataParser.Parse(packet);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMagneticField_Sensor1_ReturnsCorrectIndices()
    {
        // Arrange
        var packet = CreateValidMagDataPacket(
            dataType: MfgDataParser.TYPE_DAT,
            sensor1Mag: new[] { 12345f, 67890f, -11111f },
            sensor2Mag: new[] { 0f, 0f, 0f }
        );
        var data = MfgDataParser.Parse(packet)!.Value;

        // Act
        var magField = MfgDataParser.GetMagneticField(data, sensorIndex: 1);

        // Assert
        Assert.NotNull(magField);
        Assert.Equal(12345f, magField[0], precision: 1);
        Assert.Equal(67890f, magField[1], precision: 1);
        Assert.Equal(-11111f, magField[2], precision: 1);
    }

    [Fact]
    public void GetMagneticField_Sensor2_ReturnsCorrectIndices()
    {
        // Arrange
        var packet = CreateValidMagDataPacket(
            dataType: MfgDataParser.TYPE_DAT,
            sensor1Mag: new[] { 0f, 0f, 0f },
            sensor2Mag: new[] { 54321f, -9876f, 5555f }
        );
        var data = MfgDataParser.Parse(packet)!.Value;

        // Act
        var magField = MfgDataParser.GetMagneticField(data, sensorIndex: 2);

        // Assert
        Assert.NotNull(magField);
        Assert.Equal(54321f, magField[0], precision: 1);
        Assert.Equal(-9876f, magField[1], precision: 1);
        Assert.Equal(5555f, magField[2], precision: 1);
    }

    [Fact]
    public void GetMagneticField_NonDatType_ReturnsNull()
    {
        // Arrange - Use TYPE_REP instead of TYPE_DAT
        var packet = CreateValidMagDataPacket(
            dataType: MfgDataParser.TYPE_REP,
            sensor1Mag: new[] { 1f, 2f, 3f },
            sensor2Mag: new[] { 4f, 5f, 6f }
        );
        var data = MfgDataParser.Parse(packet)!.Value;

        // Act
        var magField = MfgDataParser.GetMagneticField(data, sensorIndex: 1);

        // Assert
        Assert.Null(magField);
    }

    [Fact]
    public void GetMagneticField_InvalidSensorIndex_ReturnsNull()
    {
        // Arrange
        var packet = CreateValidMagDataPacket(
            dataType: MfgDataParser.TYPE_DAT,
            sensor1Mag: new[] { 1f, 2f, 3f },
            sensor2Mag: new[] { 4f, 5f, 6f }
        );
        var data = MfgDataParser.Parse(packet)!.Value;

        // Act
        var magField = MfgDataParser.GetMagneticField(data, sensorIndex: 3);

        // Assert
        Assert.Null(magField);
    }

    [Fact]
    public void GetTemperatures_DatType_ReturnsTemperatures()
    {
        // Arrange
        var packet = new byte[72];
        int offset = 0;
        
        // DataType
        Buffer.BlockCopy(BitConverter.GetBytes(MfgDataParser.TYPE_DAT), 0, packet, offset, 4);
        offset += 4;
        
        // L[3]
        offset += 12;
        
        // F[0] = Sensor1 Temp, F[1] = Electronics Temp
        Buffer.BlockCopy(BitConverter.GetBytes(25.5f), 0, packet, offset, 4);
        offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(30.0f), 0, packet, offset, 4);
        offset += 4;
        
        // F[2-6] padding
        offset += 20;
        
        // F[7] = Sensor2 Temp
        Buffer.BlockCopy(BitConverter.GetBytes(26.0f), 0, packet, offset, 4);

        var data = MfgDataParser.Parse(packet)!.Value;

        // Act
        var temps = MfgDataParser.GetTemperatures(data);

        // Assert
        Assert.NotNull(temps);
        Assert.Equal(25.5f, temps.Value.sensor1Temp, precision: 1);
        Assert.Equal(30.0f, temps.Value.electronicsTemp, precision: 1);
        Assert.Equal(26.0f, temps.Value.sensor2Temp, precision: 1);
    }

    [Fact]
    public void GetTemperatures_NonDatType_ReturnsNull()
    {
        // Arrange
        var packet = CreateValidMagDataPacket(
            dataType: MfgDataParser.TYPE_POS,
            sensor1Mag: new[] { 0f, 0f, 0f },
            sensor2Mag: new[] { 0f, 0f, 0f }
        );
        var data = MfgDataParser.Parse(packet)!.Value;

        // Act
        var temps = MfgDataParser.GetTemperatures(data);

        // Assert
        Assert.Null(temps);
    }

    [Theory]
    [InlineData(MfgDataParser.TYPE_DAT)]
    [InlineData(MfgDataParser.TYPE_REP)]
    [InlineData(MfgDataParser.TYPE_POS)]
    [InlineData(MfgDataParser.TYPE_SDS)]
    [InlineData(MfgDataParser.TYPE_LOG)]
    public void Parse_AllDataTypes_ParsesCorrectly(int dataType)
    {
        // Arrange
        var packet = CreateValidMagDataPacket(
            dataType: dataType,
            sensor1Mag: new[] { 0f, 0f, 0f },
            sensor2Mag: new[] { 0f, 0f, 0f }
        );

        // Act
        var result = MfgDataParser.Parse(packet);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataType, result.Value.DataType);
    }

    /// <summary>
    /// Helper to create a valid MFG data packet
    /// </summary>
    private static byte[] CreateValidMagDataPacket(int dataType, float[] sensor1Mag, float[] sensor2Mag)
    {
        var packet = new byte[72];
        int offset = 0;

        // DataType (4 bytes)
        Buffer.BlockCopy(BitConverter.GetBytes(dataType), 0, packet, offset, 4);
        offset += 4;

        // L[3] - 3 x int32 (12 bytes)
        offset += 12;

        // F[0-7] - padding/temperatures (8 x 4 = 32 bytes)
        offset += 32;

        // F[8-10] - Sensor 1 mag data
        Buffer.BlockCopy(BitConverter.GetBytes(sensor1Mag[0]), 0, packet, offset, 4);
        offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(sensor1Mag[1]), 0, packet, offset, 4);
        offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(sensor1Mag[2]), 0, packet, offset, 4);
        offset += 4;

        // F[11-13] - Sensor 2 mag data
        Buffer.BlockCopy(BitConverter.GetBytes(sensor2Mag[0]), 0, packet, offset, 4);
        offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(sensor2Mag[1]), 0, packet, offset, 4);
        offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(sensor2Mag[2]), 0, packet, offset, 4);

        return packet;
    }
}
