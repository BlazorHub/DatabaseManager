﻿ALTER TABLE WELL_LOG_CURVE_VALUE
ALTER COLUMN MEASURED_VALUE NUMERIC(18,2);
GO
ALTER TABLE WELL_LOG_CURVE_VALUE
ALTER COLUMN INDEX_VALUE NUMERIC(18,2);
GO
ALTER TABLE STRAT_WELL_SECTION DROP CONSTRAINT STWS_W_FK;
GO
ALTER TABLE STRAT_WELL_SECTION ADD CONSTRAINT STWS_W_FK FOREIGN KEY (UWI) REFERENCES WELL(UWI) ON DELETE CASCADE;
GO
ALTER TABLE WELL_LOG_CURVE DROP CONSTRAINT WLC_W_FK;
GO
ALTER TABLE WELL_LOG_CURVE ADD CONSTRAINT WLC_W_FK FOREIGN KEY (UWI) REFERENCES WELL(UWI) ON DELETE CASCADE;
GO
ALTER TABLE WELL_LOG_CURVE_VALUE DROP CONSTRAINT WLCV_WLC_FK;
GO
ALTER TABLE WELL_LOG_CURVE_VALUE ADD CONSTRAINT WLCV_WLC_FK FOREIGN KEY (UWI) REFERENCES WELL(UWI) ON DELETE CASCADE;