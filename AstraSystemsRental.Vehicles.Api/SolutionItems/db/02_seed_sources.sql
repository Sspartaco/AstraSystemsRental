USE [AstraSystemsRental];
GO

MERGE [vehicles].[ValuationSources] AS target
USING (VALUES
    ('Fasecolda',           N'Fasecolda',            1, 10),
    ('RevistaMotor',        N'Revista Motor',        1, 20),
    ('Tucarro',             N'TuCarro',              1, 30),
    ('AsoUsados',           N'AutoExpo',             1, 40),
    ('OtrasFuentes',        N'Otras Fuentes',        0, 50)
) AS source ([Code], [DisplayName], [IsActive], [SortOrder])
ON target.[Code] = source.[Code]
WHEN MATCHED THEN
    UPDATE SET [DisplayName] = source.[DisplayName], [IsActive] = source.[IsActive], [SortOrder] = source.[SortOrder]
WHEN NOT MATCHED THEN
    INSERT ([Code], [DisplayName], [IsActive], [SortOrder])
    VALUES (source.[Code], source.[DisplayName], source.[IsActive], source.[SortOrder]);
GO

UPDATE [vehicles].[ValuationSources] SET [IsActive] = 0 WHERE [Code] IN ('MercadoLibre', 'FacebookMarketplace');
GO
