USE Frenetikmove
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lg_insert ON lg WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO FC (fcstamp,lgstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,estab,nome,cm,cmdesc,origem,deb,edeb,debf,edebf,adoc,dataven,fref,intid,ccusto,ncusto,pais,obs,zona) select inserted.lgstamp,inserted.lgstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.data,inserted.no,inserted.estab,inserted.nome,31,(select cmdesc from cm1 (NOLOCK) where cm=31),'LG',inserted.totalcc-inserted.arred,inserted.etotalcc-inserted.earred,inserted.totalcc-inserted.arred,inserted.etotalcc-inserted.earred,rtrim(convert(char(10),inserted.leno)),inserted.dataven,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.pais,ltrim(inserted.fcdesc),fl.zona from inserted inner join fl on inserted.no=fl.no and inserted.estab=fl.estab where inserted.totalcc<>0 or inserted.etotalcc<>0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_li_delete ON li WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE CC SET cc.debf=cc.debf-case when deleted.rec>0 then deleted.rec-deleted.arred else 0 end from deleted where deleted.ccstamp=cc.ccstampupdate cc set cc.credf=cc.credf+case when deleted.rec<0 then deleted.rec-deleted.arred else 0 end from deleted where deleted.ccstamp=cc.ccstampupdate cc set cc.edebf=cc.edebf-case when deleted.erec>0 then deleted.erec-deleted.earred else 0 end from deleted where deleted.ccstamp=cc.ccstampupdate cc set cc.ecredf=cc.ecredf+case when deleted.erec<0 then deleted.erec-deleted.earred else 0 end from deleted where deleted.ccstamp=cc.ccstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tno_insert ON tno WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftma_insert ON ftma WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome=case when ft.tipodoc=3 then '' else inserted.nome end,ma.no=case when ft.tipodoc=3 then 0 else inserted.no end,ma.ftnmdoc=case when ft.tipodoc=3 then ma.ftnmdoc else inserted.nmdoc end,ma.ftfno=case when ft.tipodoc=3 then ma.ftfno else inserted.fno end ,ma.ftfdata=case when ft.tipodoc=3 then ma.ftfdata else inserted.fdata end, ma.ftstamp=case when ft.tipodoc=3 then ma.ftstamp else inserted.ftstamp end,ma.casa=case when ft.tipodoc=3 then 1 else 0 end,ma.morada=case when ft.tipodoc=3 then '' else ft.morada end,ma.local=case when ft.tipodoc=3 then '' else ft.local end,ma.codpost=case when ft.tipodoc=3 then '' else ft.codpost end,ma.zona=case when ft.tipodoc=3 then '' else ft.zona end, ma.vendnm=case when ft.tipodoc=3 then '' else ft.vendnm end,ma.vendedor=case when ft.tipodoc=3 then 0 else ft.vendedor end,ma.instal=case when ft.tipodoc=3 then ma.instal else ft.fdata end,ma.telefone=case when ft.tipodoc=3 then '' else cl.telefone end,ma.contacto=case when ft.tipodoc=3 then '' else cl.contacto end,ma.estab= case when ft.tipodoc=3 then 0 else inserted.estab end,ma.email= case when ft.tipodoc=3 then '' else cl.email end,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.fdata,121)+')'  end, ma.ftorigem=case when ft.tipodoc=3 then ma.ftorigem else 'FT' end, ma.noarm=case when ft.tipodoc=3 then inserted.armazem else ma.noarm end, ma.armdatain=case when ft.tipodoc=3 then inserted.fdata else ma.armdatain end from inserted inner join ft on inserted.ftstamp=ft.ftstamp inner join cl on cl.no=ft.no and cl.estab=ft.estab where inserted.mastamp=ma.mastamp  IF (select COUNT(*) from inserted where inserted.serprvef = 1) > 0 UPDATE MA SET ma.serie = inserted.serie from inserted where inserted.serprvef = 1 and inserted.mastamp=ma.mastamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lo_delete ON lo WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from OD from deleted where od.lostamp=deleted.lostampDELETE from FC from deleted where fc.lostamp=deleted.lostampDELETE from OL from deleted where ol.lostamp=deleted.lostamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_olbc_insert ON olbc WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (transf,olstamp,olbcstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,intid,ccusto,ncusto,dvalor,local,ollocal,contado,data,descricao,said,esaid,saidm,moeda,olcodigo,grupo,sgrupo,cheque,documento,operext,cecope )select 1,inserted.olbcstamp,inserted.olbcstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BC',inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.dvalor,'B',inserted.ollocal,inserted.contado,inserted.data,inserted.descricao,inserted.valor,inserted.evalor,inserted.valorm,inserted.moeda,'Trf','Transferências','Transferências',inserted.cheque,inserted.documento,inserted.operext,inserted.cecopefrom inserted where inserted.contado>0 INSERT INTO OL (transf,olstamp,olbcstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,intid,ccusto,ncusto,dvalor,local,ollocal,contado,data,descricao,entr,eentr,moeda,olcodigo,grupo,sgrupo,cheque,documento,operext,cecope )select 1,substring('C'+inserted.olbcstamp,1,25),inserted.olbcstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BC',inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.dvalor,'C','Caixa',0,inserted.data,inserted.descricao,inserted.valor,inserted.evalor,'PTE ou EURO','Trf','Transferências','Transferências',inserted.cheque,inserted.documento,inserted.operext,inserted.cecopefrom inserted where inserted.contado>0 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_owli_insert ON OWLI WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF (select count(*) from ow inner join inserted on inserted.owstamp = ow.owstamp where ow.ozstamp='' and ow.postamp='' and ow.restamp='' and ow.ollinhas=1 and ow.owliolcod=1) > 0INSERT INTO OL (olstamp,owlistamp,owstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,entrm,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext,cecope) select inserted.owlistamp,inserted.owlistamp,inserted.owstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'OW',ow.docnome,ow.cheque,ow.data,ow.dvalor,(case when dcli.deb = 0 then inserted.valor else 0 end),(case when dcli.deb = 0 then inserted.evalor else 0 end),(case when dcli.deb = 1 then inserted.valor else 0 end),(case when dcli.deb = 1 then inserted.evalor else 0 end),(case when dcli.deb = 0 then inserted.valorm2 else 0 end),(case when dcli.deb = 1 then inserted.valorm2 else 0 end),ow.moeda2,inserted.oldesc,inserted.olcodigo,inserted.grupo,inserted.sgrupo,ow.fref,ow.intid,inserted.ccusto,ow.ncusto,ow.ollocal,ow.contado,ow.local,ow.site,ow.pnome,ow.pno,ow.cxstamp,ow.cxusername,ow.ssstamp,ow.ssusername,ow.operext,inserted.cecope from inserted inner join ow on ow.owstamp = inserted.owstamp and ow.ozstamp='' and ow.postamp='' and ow.restamp='' and ow.ollinhas=1 and ow.owliolcod=1 inner join dcli on dcli.dclistamp = inserted.dclistamp Where inserted.valor <> 0 OR inserted.evalor <> 0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oz_insert ON OZ WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (olstamp,ozstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,entrm,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,operext,cecope) select inserted.ozstamp,inserted.ozstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'OZ',inserted.docnome,inserted.cheque,inserted.data,inserted.dvalor,inserted.entr,inserted.eentr,inserted.said,inserted.esaid,inserted.entrm2,inserted.saidm2,inserted.moeda2,inserted.descricao,inserted.olcodigo,inserted.grupo,inserted.sgrupo,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.local,inserted.operext,inserted.cecope from inserted GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_Para1_update ON PARA1 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (select COUNT(*) from inserted where descricao = 't_para1.dDataUltimaT_Para1' or descricao = 't_para1.cHoraUltimaT_Para1') = 0BEGINif (select COUNT(*) from para1 (nolock) where descricao = 't_para1.dDataUltimaT_Para1') = 0BEGINinsert into para1 (descricao, para1stamp) values ('t_para1.dDataUltimaT_Para1', substring(cast(newid() as char(36)),1,25))insert into para1 (descricao, para1stamp) values ('t_para1.cHoraUltimaT_Para1', substring(cast(newid() as char(36)),1,25))ENDupdate para1 set valor = convert(varchar, getdate(), 104) where descricao = 't_para1.dDataUltimaT_Para1'update para1 set valor = convert(varchar, getdate(), 108) where descricao = 't_para1.cHoraUltimaT_Para1'ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pc_update ON PC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(descricao)
begin
update ml set ml.descricao=inserted.descricao
from inserted
where ml.conta=inserted.conta and year(ml.data)=inserted.ano
UPDATE mca SET mca.descricao=inserted.descricao
from inserted
where mca.conta=inserted.conta and mca.ano=inserted.ano
endGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ttapa_delete ON ttapa WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE tta SET tta.hreal=tta.hreal-deleted.hreal from DELETED where tta.ttastamp=deleted.ttastamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pl_insert ON pl WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONupdate fc
set fc.credf =fc.credf +isnull((select (case when sum(inserted.cambio)=0 and sum(inserted.escrec)>0 then sum(inserted.escrec)-sum(inserted.arred)
else case when sum(inserted.cambio)<>0 and  sum(inserted.escrec)>0 then sum(inserted.escrec) else 0 end end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1), 0)
, fc.debf =fc.debf -isnull((select (case when sum(inserted.cambio)=0 and sum(inserted.escrec)<0 then sum(inserted.escrec)- sum(inserted.arred)
else case when sum(inserted.cambio)<>0 and sum(inserted.escrec)<0 then sum(inserted.escrec) else 0 end end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.credfm =fc.credfm+ isnull((select (case when sum(inserted.ecambio)<>0 and sum(inserted.rec)>0 then sum(inserted.rec) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.debfm =fc.debfm -isnull((select (case when sum(inserted.ecambio)<>0 and sum(inserted.rec)<0 then sum(inserted.rec) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.ecredf =fc.ecredf +isnull((select (case when sum(inserted.erec)>0 then sum(inserted.erec)-sum(inserted.earred) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.edebf =fc.edebf -isnull((select (case when sum(inserted.erec)<0 then sum(inserted.erec)- sum(inserted.earred) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.virsreg =fc.virsreg + isnull((select sum(inserted.virs) from inserted where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.evirsreg =fc.evirsreg+ isnull((select sum(inserted.evirs) from inserted where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
from inserted
where inserted.fcstamp =fc.fcstamp and inserted.process=1

update fc
set fc.evalpo =fc.evalpo +(select sum(inserted.erec -inserted.eivacativado) from inserted where inserted.fcstamp =fc.fcstamp)
, fc.valpo =fc.valpo +(select sum(inserted.escrec -inserted.ivacativado) from inserted where inserted.fcstamp =fc.fcstamp)
, fc.mvalpo =fc.mvalpo +(select sum(inserted.rec  -inserted.mivacativado) from inserted where inserted.fcstamp =fc.fcstamp)
, fc.ultdoc ='Doc. nº '+convert(char(10),inserted.rno)+' de '+convert(char(10),inserted.rdata,104)
, fc.recibado =1
from inserted
where fc.fcstamp =inserted.fcstamp

update FC
set	fc.irsdif =fc.irsdif +(select sum(case when inserted.desconto=100 or po.fin=100 then 0 else (ROUND((inserted.virs/ (1-(case when inserted.desconto<>0
then inserted.desconto else po.fin end/100))),0)-inserted.virs) end)
from inserted
inner join po on po.postamp=inserted.postamp where inserted.fcstamp=fc.fcstamp)
, fc.eirsdif =fc.eirsdif +(select sum(case when inserted.desconto=100 or po.fin=100 then 0 else (ROUND((inserted.evirs/(1-(case when inserted.desconto<>0
then inserted.desconto else po.fin end/100))),2)-inserted.evirs) end)
from inserted
inner join po on po.postamp=inserted.postamp where inserted.fcstamp=fc.fcstamp)
from inserted
inner join po on po.postamp =inserted.postamp
where fc.fcstamp =inserted.fcstamp
and inserted.process=1 and (inserted.desconto<>0 or po.fin<>0)

if ( select count(*) from inserted where inserted.fcorigem='PD')>0 
update pd 
set pd.integrado =1
from inserted
inner join fc on fc.fcstamp =inserted.fcstamp
inner join pd on pd.pdstamp =fc.pdstamp
where fc.fcstamp =inserted.fcstamp and inserted.fcorigem='PD'GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pochf_delete ON POCHF WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE chf
SET deststamp = ''
, destino = ''
from deleted inner join chf on deleted.chfstamp = chf.chfstamp 
where deleted.cheque <> ''					
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pochf_update ON POCHF WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF update(cheque)
BEGIN 

UPDATE chf
SET deststamp = '' 
, destino = ''
from deleted 
inner join chf on deleted.chfstamp = chf.chfstamp

UPDATE chf
SET deststamp = inserted.pochfstamp 
, destino =  'POCHF' 
, nome = case when chf.nome = '' then po.nome else chf.nome end 
, no = case when chf.no = 0 then po.no else chf.no end 
, estab = case when chf.no = 0 then po.estab else chf.estab end 					
from inserted 
inner join chf on inserted.chfstamp = chf.chfstamp
inner join po on po.postamp = inserted.postamp 

END 			
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rd_delete ON rd WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM CC from deleted where cc.rdstamp=deleted.rdstampDELETE FROM CH from deleted where ch.rdstamp=deleted.rdstamp DELETE FROM OL from deleted where ol.rdstamp=deleted.rdstamp if (select isnull(count(*),0) from deleted inner join tsrd on tsrd.ndoc = deleted.ndoc where tsrd.regrd = 1) > 0  BEGIN  update cc  set cc.credf = cc.credf - (deleted.total*-1)  , cc.ecredf = cc.ecredf - (deleted.etotal*-1)  , cc.credfm =cc.credfm - (deleted.totalmoeda*-1)  from deleted,cc	where deleted.ordstamp = cc.rdstamp  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ff_insert ON ff WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON			if update(data)
				update ff set
					ff.diai = case when month(i.data)<10 then '0' else '' end + CAST(month(i.data) as varchar(2)) + case when day(i.data)<10 then '0' else '' end + CAST(day(i.data) as varchar(2))
				from inserted i where ff.ffstamp=i.ffstamp

			if update(dataf)
				update ff set
					ff.diaf = case when month(i.dataf)<10 then '0' else '' end + CAST(month(i.dataf) as varchar(2)) + case when day(i.dataf)<10 then '0' else '' end + CAST(day(i.dataf) as varchar(2))
				from inserted i where ff.ffstamp=i.ffstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rerev_update ON REREV WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF update(plano) 
BEGIN 
UPDATE re
SET re.plano=inserted.plano, re.dplano = inserted.dplano, re.dinoplano = inserted.dinoplano 
, re.diaplano = inserted.diaplano, re.dilnoplano = inserted.dilnoplano 
, re.planoonline= inserted.planoonline
, re.dostamp=inserted.dostamp
from re inner join inserted on re.restamp = inserted.restamp 			
ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sfcnome_delete ON sfcnome WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM SFCOR FROM deleted where sfcor.nome=deleted.campoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sgt_delete ON SGT WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete sx from sx inner join deleted on sx.ref=deleted.ref where sx.tam=deleted.tamGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
 CREATE PROCEDURE [dbo].[usp_RepararUserDboOrfao] WITH ENCRYPTIONAS  SET NOCOUNT ON;     declare @owner_sid varbinary(85);
    declare @owner_login NVARCHAR(128);
    declare @bdnome NVARCHAR(128) = DB_NAME();
    declare @dbo_sid varbinary(85);
    declare @dbo_login NVARCHAR(128);
    declare @sql NVARCHAR(max);
    
    select @owner_sid = owner_sid,
    		@owner_login = SUSER_SNAME(owner_sid)
    from sys.databases
    where name = DB_NAME();

    select  @owner_sid, @owner_login
    -- Obter SID do usuário dbo
    select @dbo_sid = dp.SID
    from sys.database_principals dp
    where dp.name = 'dbo';

    -- Verifica se existe login associado a esse SID
    select @dbo_login = sp.name
    from sys.server_principals sp
    where sp.SID = @dbo_sid;

    -- se não há login associado, então o dbo está órfão
    if @dbo_login is null
    begin
    	set @sql = 'ALTER AUTHORIZATION ON DATABASE::[' + @bdnome + '] TO [sa];';

    	begin try
    		exec sp_executesql @sql;
    	end try
    	begin catch
    		declare @errmsg NVARCHAR(4000) = ERROR_MESSAGE();
    		declare @errnum int = ERROR_NUMBER();
    		declare @errstate int = ERROR_STATE();

    		RAISERROR(@errmsg, 16, @errstate);
    	end catch
    end
    -- else
    -- begin
    	-- print 'Usuário dbo está associado ao login: ' + @dbo_login;
    -- end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
 CREATE PROCEDURE [dbo].[usp_LimparTabelasTemporariasSpid] WITH ENCRYPTION, EXECUTE AS OWNERAS  SET NOCOUNT ON;     declare @spid NVARCHAR(10) = CAST(@@SPID AS NVARCHAR(10));
    declare @sql NVARCHAR(MAX) = '';

    select @sql = COALESCE(@sql, '') + 'DROP TABLE [' + s.name + '].[' + t.name + '];' + CHAR(13)
    from sys.tables t
    join sys.schemas s on t.schema_id = s.schema_id
    where t.name = 'tmpx_' + @spid
       OR t.name = 'tmpxapoio_' + @spid;

    if @sql <> ''
        exec sp_executesql @sql;
        
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_st_delete ON ST WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIF (select count(*) from bc inner join deleted on bc.ststamp=deleted.ststamp )>0 BEGINdelete bc from deleted where bc.ststamp=deleted.ststampENDIF (select count(*) from deleted where deleted.stns=1 and deleted.iecaisref=1)>0 BEGINupdate st set st.iecaref=SPACE(18),st.iecarefnome=SPACE(60) from st inner join deleted on st.iecaref=deleted.ref where st.iecasug=1ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_stfami_update ON STFAMI WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (nome) BEGIN update st set st.faminome=inserted.nome from inserted where st.familia=inserted.ref END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_stic_delete ON stic WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from SL from deletedwhere sl.sticstamp=deleted.sticstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sv_delete ON sv WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTDECLARE @@STICLASTDATE datetimeSELECT @@STICLASTDATE = (select case when exists(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc)  then convert(datetime,(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc),121) else convert(datetime,'19000101',121) end) IF @@MROW = 1 BEGIN IF exists(select * from deleted where deleted.data < @@STICLASTDATE)RETURN END UPDATE ST SET st.pcpond=case when st.stock>0 then case when deleted.cm<50 then (st.pcpond*st.stock-deleted.tt)/(st.stock) else (st.pcpond*st.stock+deleted.tt)/(st.stock) end else st.pcpond end,st.epcpond=case when st.stock>0 then case when deleted.cm<50 then (st.epcpond*st.stock-deleted.ett)/(st.stock) else (st.epcpond*st.stock+deleted.ett)/(st.stock) end else st.epcpond end from deleted where st.ref=deleted.ref  and deleted.data >= @@STICLASTDATE GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sv_update ON sv WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (ref) or update (tt) or update (cm) or update (lote) or update (data)  BEGIN DECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTDECLARE @@STICLASTDATE datetimeSELECT @@STICLASTDATE = (select case when exists(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc)  then convert(datetime,(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc),121) else convert(datetime,'19000101',121) end) IF @@MROW = 1 BEGIN IF exists(select * from deleted where deleted.data < @@STICLASTDATE)RETURN END UPDATE ST SET st.pcpond=case when st.stock>0 then case when deleted.cm<50 then (st.pcpond*st.stock-deleted.tt)/(st.stock) else (st.pcpond*st.stock+deleted.tt)/(st.stock) end else st.pcpond end,st.epcpond=case when st.stock>0 then case when deleted.cm<50 then (st.epcpond*st.stock-deleted.ett)/(st.stock) else (st.epcpond*st.stock+deleted.ett)/(st.stock) end else st.epcpond end from deleted where st.ref=deleted.ref  and deleted.data >= @@STICLASTDATE IF @@MROW = 1 BEGIN IF exists(select * from inserted where inserted.data < @@STICLASTDATE)RETURN END INSERT INTO SA (sastamp,armazem,ref) select convert(char(5),armazem)+ref,armazem,ref from inserted where not EXISTS(select * from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and inserted.data >= @@STICLASTDATE UPDATE ST SET st.pcpond=case when st.stock>0  then case when inserted.cm<50 then (st.pcpond*st.stock+inserted.tt)/(st.stock) else (st.pcpond*st.stock-inserted.tt)/(st.stock) end else st.pcpond end,st.epcpond=case when st.stock>0  then case when inserted.cm<50 then (st.epcpond*st.stock+inserted.ett)/(st.stock) else (st.epcpond*st.stock-inserted.ett)/(st.stock) end else st.epcpond end  from INSERTED  inner join cm2 on cm2.cm = inserted.cm where (cm2.MUDAPCPOND = 1 or cm2.MUDAPCULT=1) and st.ref=inserted.ref   and inserted.data >= @@STICLASTDATE  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cet_update ON CET WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (update(nome) or update(tipo)) and (select count(*) from rc4 inner join inserted on rc4.cetstamp=inserted.cetstamp)>0 begin update rc4 set rc4.nmcet=inserted.nome,rc4.tipo=inserted.tipo from inserted where rc4.cetstamp=inserted.cetstamp endif (update(nome) or update(tipo)) and (select count(*) from et1 inner join inserted on et1.cetstamp=inserted.cetstamp)>0 begin update et1 set et1.nmcet=inserted.nome,et1.tipo=inserted.tipo from inserted where et1.cetstamp=inserted.cetstamp end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tn_iurgente ON TN WITH ENCRYPTION FOR UPDATE,INSERT  AS SET NOCOUNT ON IF update (iurgente) BEGIN update tn set tn.eurgente=inserted.eurgente from inserted where tn.oristamp=inserted.oristamp END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ba_update ON ba WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE (entrada) or UPDATE(saida) or UPDATE(eentrada) or UPDATE(esaida) or UPDATE(contado) or UPDATE(reco) UPDATE BL SET bl.saldo = bl.saldo-(deleted.entrada-deleted.saida), bl.esaldo = bl.esaldo-(deleted.eentrada-deleted.esaida), bl.saldom = bl.saldom-(deleted.entradam-deleted.saidam), bl.rsaldo = bl.rsaldo-(case when deleted.reco=1 then deleted.entrada-deleted.saida else 0 end), bl.ersaldo = bl.ersaldo-(case when deleted.reco=1 then deleted.eentrada-deleted.esaida else 0 end), bl.rsaldom = bl.rsaldom-(case when deleted.reco=1 then deleted.entradam-deleted.saidam else 0 end) from DELETED where bl.noconta=deleted.contadoIF UPDATE (entrada) or UPDATE(saida) or UPDATE(eentrada) or UPDATE(esaida) or UPDATE(entradam) or UPDATE(saidam) or UPDATE(contado) or UPDATE(reco) UPDATE BL SET bl.saldo = bl.saldo+inserted.entrada-inserted.saida, bl.esaldo = bl.esaldo+inserted.eentrada-inserted.esaida, bl.saldom = bl.saldom+inserted.entradam-inserted.saidam, bl.rsaldo = bl.rsaldo+case when inserted.reco=1 then inserted.entrada-inserted.saida else 0 end, bl.ersaldo = bl.ersaldo+case when inserted.reco=1 then inserted.eentrada-inserted.esaida else 0 end, bl.rsaldom = bl.rsaldom+case when inserted.reco=1 then inserted.entradam-inserted.saidam else 0 end, bl.datasaldo = inserted.data from INSERTED where bl.noconta=inserted.contadoIF UPDATE (reco) or UPDATE(extracto)  UPDATE ML SET ml.reco=inserted.reco,ml.extracto=inserted.extracto from inserted where inserted.bastamp=ml.bastamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_faca_insert ON faca WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE FAC SET fac.adiantado=fac.adiantado+(select sum(inserted.valor) from inserted where fac.facstamp=inserted.facstamp),fac.eadiantado=fac.eadiantado+(select sum(inserted.evalor) from inserted where fac.facstamp=inserted.facstamp) from inserted where fac.facstamp=inserted.facstampif (select count(*) from inserted where inserted.valor<>0 or inserted.evalor<>0)>0INSERT INTO OL (olstamp,facastamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,moeda,descricao,olcodigo,grupo,sgrupo,ollocal,contado,local) select inserted.facastamp,inserted.facastamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'FACA',inserted.cdesc,convert(char(15),inserted.nrdoc),inserted.data,inserted.data,inserted.valor-inserted.juros,inserted.evalor-inserted.ejuros,'PTE ou EURO',inserted.obs,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.ollocal,inserted.contado,inserted.telocal from inserted inner join oc on oc.olcodigo=inserted.olcodigo where inserted.valor<>0 or inserted.evalor<>0
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bo_insert ON bo WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF EXISTS(SELECT 1 FROM bo2 as tempbo2 
INNER JOIN inserted ON inserted.bostamp COLLATE Latin1_General_CI_AI = tempbo2.bo2stamp COLLATE Latin1_General_CI_AI 
and tempbo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN ts on inserted.ndos = ts.ndos
INNER JOIN ts2 on ts.tsstamp COLLATE Latin1_General_CI_AI = ts2.ts2stamp COLLATE Latin1_General_CI_AI
WHERE ts2.assinaimp = 0
AND ts2.docsimport = 0
AND ts2.assinadoc = 1
AND inserted.obrano>0)
BEGIN
DECLARE @ErrorMessage NVARCHAR(2000) 
DECLARE @ErrorSeverity INT 
DECLARE @ErrorState INT 
SET @ErrorSeverity = 16
SET @ErrorState = 1
SELECT TOP 1 
@ErrorMessage = 'A assinatura digital no dossier interno ' + rtrim(inserted.nmdos) + ' n.º ' + cast(inserted.obrano as varchar(25)) +  ' está vazia. Por favor contacte o administrador do sistema.' 
FROM bo2 as tempbo2 
INNER JOIN inserted ON inserted.bostamp COLLATE Latin1_General_CI_AI = tempbo2.bo2stamp COLLATE Latin1_General_CI_AI 
and tempbo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN ts on inserted.ndos = ts.ndos
INNER JOIN ts2 on ts.tsstamp COLLATE Latin1_General_CI_AI = ts2.ts2stamp COLLATE Latin1_General_CI_AI
WHERE ts2.assinaimp = 0
AND ts2.docsimport = 0
AND ts2.assinadoc = 1
AND inserted.obrano>0
RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
ROLLBACK 
END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facc_delete ON facc WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.faccstamp=deleted.faccstamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cpt_insert ON cpt WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON			update cptng set
				cptng.nomecpt=i.nome
			from inserted as i
			where cptng.cptstamp=i.cptstamp

			update cptclp set
				cptclp.nomecpt=i.nome
			from inserted as i
			where cptclp.cptstamp=i.cptstamp

			update cptst set
				cptst.nomecpt=i.nome
			from inserted as i
			where cptst.cptstamp=i.cptstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cpt_delete ON cpt WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON			delete from cptng where cptng.cptstamp=(select cptstamp from deleted)
			delete from cptclp where cptclp.cptstamp=(select cptstamp from deleted)
			delete from cptst where cptst.cptstamp=(select cptstamp from deleted)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facl_insert ON facl WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE CC SET cc.faccstamp=inserted.faccstamp,cc.faclstamp=inserted.faclstamp from inserted where cc.ccstamp=inserted.ccstampUPDATE FACC SET facc.totalrec=facc.totalrec+(select sum(inserted.escrec) from inserted where facc.facstamp=inserted.facstamp),facc.etotalrec=facc.etotalrec+(select sum(inserted.erec) from inserted where facc.facstamp=inserted.facstamp) from inserted where facc.facstamp=inserted.facstampUPDATE FAC SET fac.cessionado=fac.cessionado+(select sum(inserted.escval-inserted.escrec) from inserted where fac.facstamp=inserted.facstamp),fac.ecessionado=fac.ecessionado+(select sum(inserted.eval-inserted.erec) from inserted where fac.facstamp=inserted.facstamp) from inserted where fac.facstamp=inserted.facstampUPDATE FAL SET fal.cessionado=fal.cessionado+(select sum(inserted.escval-inserted.escrec) from inserted where fal.falstamp=inserted.falstamp),fal.ecessionado=fal.ecessionado+(select sum(inserted.eval-inserted.erec) from inserted where fal.falstamp=inserted.falstamp) from inserted where fal.falstamp=inserted.falstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cts_update ON CTS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON				if update(nome)
				begin
					update vi set
						vi.ctacto=i.nome
					from inserted as i inner join vi on vi.nocts=i.nocts
					where vi.nocts=i.nocts
				end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vl_insert ON vl WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE VY SET vy.valornp=vy.valornp-inserted.rec,vy.evalornp=vy.evalornp-inserted.erec from inserted where inserted.vystamp=vy.vystamp and inserted.process=1 UPDATE VY SET vy.ultdoc=rtrim('Doc. nº '+convert(char(10),inserted.rno))+' de '+convert(char(10),inserted.rdata,104),vy.recibado=1 from inserted where vy.vystamp=inserted.vystamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vl_update ON vl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE VY SET vy.valornp=vy.valornp+deleted.rec,vy.evalornp=vy.evalornp+deleted.erec from deleted where deleted.vystamp=vy.vystamp and deleted.process=1 UPDATE VY SET vy.ultdoc='',vy.recibado=0 from deleted where vy.vystamp=deleted.vystamp UPDATE VY SET vy.valornp=vy.valornp-inserted.rec,vy.evalornp=vy.evalornp-inserted.erec from inserted where inserted.vystamp=vy.vystamp and inserted.process=1 UPDATE VY SET vy.ultdoc=rtrim('Doc. nº '+convert(char(10),inserted.rno))+' de '+convert(char(10),inserted.rdata,104),vy.recibado=1 from inserted where vy.vystamp=inserted.vystamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cb_delete ON CB WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE cbpaises from cbpaises
INNER JOIN deleted ON cbpaises.pais = deleted.pais and cbpaises.moeda = deleted.moeda AND NOT EXISTS (SELECT * FROM cb WHERE cb.pais = deleted.pais and cb.moeda = deleted.moeda)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_foma_delete ON foma WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM MA FROM deleted inner join fo on deleted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where ma.fomastamp=deleted.fomastamp and cm1.debito=0 UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.casa=1,ma.indisponivel=0,ma.ftorigem='',ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='', ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.data,121))+4,datalength(ma.obs)) from deleted inner join fo on deleted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where deleted.mastamp=ma.mastamp and cm1.debito=1 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tda_update ON tda WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tda_insert ON tda WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ri_delete ON ri WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE BI SET bi.ncinteg = '' from deleted where deleted.ristamp=bi.ncintegUPDATE FI SET fi.ncinteg = '' from deleted where deleted.ristamp=fi.ncintegUPDATE FN SET fn.ncinteg = '' from deleted where deleted.ristamp=fn.ncinteg
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_e1_update2 ON E1 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (nomecomp) BEGIN update e3 set e3.nomecomp=inserted.nomecomp from inserted,deleted where deleted.estab=0 END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tbu_insert ON tbu WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET tpo.ebentr=tpo.ebentr+inserted.eentr, tpo.ebsaid=tpo.ebsaid+inserted.esaid, tpo.bentr=tpo.bentr+inserted.entr, tpo.bsaid=tpo.bsaid+inserted.said, TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where tpo.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_do_delete ON DO WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE di 
SET di.edeb = di.edeb - ISNULL((SELECT SUM(deleted.edebana+deleted.edebord+deleted.edebfin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0)
, di.deb = di.deb - ISNULL((SELECT SUM(deleted.debana+deleted.debord+deleted.debfin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0) 			
, di.cre=di.cre - ISNULL((SELECT SUM(deleted.creana+deleted.creord+deleted.crefin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0) 	
, di.ecre=di.ecre - ISNULL((SELECT SUM(deleted.ecreana+deleted.ecreord+deleted.ecrefin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0)  		
FROM deleted INNER JOIN di on deleted.dino=di.dino and di.diano = deleted.ano			GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pr_delete ON PR WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.prstamp=deleted.prstampdelete from prcc from deleted where prcc.prstamp=deleted.prstampUPDATE HS SET hs.processada=0 from DELETED where hs.prstamp=deleted.prstampUPDATE FP SET fp.processada=0 from DELETED where fp.prstamp=deleted.prstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pe_update ON PE WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE(area)BEGINUPDATE pr SET pr.area = inserted.area FROM inserted INNER JOIN pr ON inserted.no = pr.noUPDATE fd SET fd.area = inserted.area FROM inserted INNER JOIN fd ON inserted.no = fd.noUPDATE hp SET hp.area = inserted.area FROM inserted INNER JOIN hp ON inserted.no = hp.noUPDATE hs SET hs.area = inserted.area FROM inserted INNER JOIN hs ON inserted.no = hs.noUPDATE fr SET fr.area = inserted.area FROM inserted INNER JOIN fr ON inserted.no = fr.noUPDATE ds SET ds.area = inserted.area FROM inserted INNER JOIN ds ON inserted.no = ds.noEND
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cm3_delete ON CM3 WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate cl set cl.vendedor=0, cl.vendnm='' from deleted where cl.vendedor=deleted.cmGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sfcnome_update ON sfcnome WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE SFCOR set sfcor.nome=inserted.campofrom deleted inner join inserted on inserted.sfcnomestamp =deleted.sfcnomestamp  where sfcor.nome=deleted.campoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fi_delete ON fi WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM SL FROM deleted where sl.fistamp=deleted.fistamp and deleted.fistamp<>''DELETE FROM SV FROM deleted where sv.fistamp=deleted.fistamp and deleted.fistamp<>''UPDATE BI SET bi.partes2 = bi.partes2-(select SUM(deleted.partes) from deleted where deleted.bistamp <> '' and deleted.bistamp = bi.bistamp) from bi inner join deleted on deleted.bistamp = bi.bistamp inner join ts (nolock) on ts.ndos = bi.ndos where deleted.bistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1)UPDATE FTREG SET  ftreg.vciva=ftreg.vciva-  (select ROUND(sum(case when deleted.ivaincl=1 then ((deleted.tiliquido)*-1)*(1-ftreg.fin/100) else -((deleted.tiliquido-round((deleted.tiliquido*ftreg.fin/100),0))*(1+deleted.iva/100)) end),0)  from deleted, ftreg where deleted.ftregstamp=ftreg.ftregstamp),   ftreg.vmoeda=ftreg.vmoeda-  (select ROUND(sum(case when deleted.ivaincl=1 then ((deleted.tmoeda)*-1)*(1-ftreg.fin/100) else -((deleted.tmoeda-round((deleted.tmoeda*ftreg.fin/100),2))*(1+deleted.iva/100)) end),2)   from deleted, ftreg where deleted.ftregstamp=ftreg.ftregstamp),   ftreg.evciva=ftreg.evciva-  (select ROUND(sum(case when deleted.ivaincl=1 then ((deleted.etiliquido)*-1)*(1-ftreg.fin/100) else -((deleted.etiliquido-round((deleted.etiliquido*ftreg.fin/100),2))*(1+deleted.iva/100)) end),2)  from deleted, ftreg where deleted.ftregstamp=ftreg.ftregstamp),  ftreg.ultdoc='',  ftreg.fin = isnull((select top 1 ft.fin from ft left join deleted on deleted.ftstamp=ft.ftstamp  where deleted.ftregstamp=ftreg.ftregstamp),0)  from ftreg inner join deleted on ftreg.ftregstamp=deleted.ftregstamp where deleted.tipodoc=3 and deleted.ftregstamp<>space(25)UPDATE FT SET ft.facturada = 0, ft.nmdocft = '', ft.fnoft =0  from deleted where deleted.ndocft<>0 and deleted.ndocft = ft.ndoc and deleted.fnoft = ft.fno  and deleted.ftanoft = ft.ftano  and ((select count(*) from fi where fi.fistamp<>deleted.fistamp and fi.ndocft=deleted.ndocft and fi.fnoft=deleted.fnoft and fi.ftanoft=deleted.ftanoft and fi.ftstamp=deleted.ftstamp)+(select count(*) from inserted where inserted.ndocft=deleted.ndocft and inserted.fnoft=deleted.fnoft and inserted.ftanoft=deleted.ftanoft))=0 UPDATE FT SET ft.facturada = 0, ft.nmdocft = '', ft.fnoft =0  from deleted where deleted.ndocft<>0 and deleted.ndocft = ft.ndoc and deleted.fnoft = ft.fno  and deleted.ftanoft = ft.ftano  and ((select count(*) from fi where fi.fistamp<>deleted.fistamp and fi.ndocft=deleted.ndocft and fi.fnoft=deleted.fnoft and fi.ftanoft=deleted.ftanoft and fi.ftstamp=deleted.ftstamp)+(select count(*) from inserted where inserted.ndocft=deleted.ndocft and inserted.fnoft=deleted.fnoft and inserted.ftanoft=deleted.ftanoft))=0 DELETE FROM PN FROM deleted where pn.fistamp=deleted.fistampUPDATE FT SET  ft.custo=ft.custo-(select case when deleted.tipodoc=3 then sum(-deleted.qtt*deleted.custo) else sum(deleted.qtt*deleted.custo) end  from deleted where deleted.ftstamp=ft.ftstamp and deleted.composto=0 group by deleted.tipodoc) ,  ft.ecusto=ft.ecusto-(select case when deleted.tipodoc=3 then sum(-deleted.qtt*deleted.ecusto) else sum(deleted.qtt*deleted.ecusto) end  from deleted where deleted.ftstamp=ft.ftstamp and deleted.composto=0 group by deleted.tipodoc) from deleted where deleted.ftstamp=ft.ftstamp and deleted.composto=0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fl_update ON fl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (select count(*) from inserted where inserted.forvd=0)>0 BEGINIF (select count(*) from inserted inner join deleted on inserted.flstamp=deleted.flstamp where inserted.nome<>deleted.nome )>0 BEGINIF (select count(*) from fc inner join inserted on fc.no=inserted.no and fc.estab=inserted.estab and inserted.forvd=0 )>0 update fc set fc.nome=inserted.nome from inserted where fc.no=inserted.no and fc.estab=inserted.estab and inserted.forvd=0 IF (select count(*) from fo inner join inserted on fo.no=inserted.no and fo.estab=inserted.estab and inserted.forvd=0 )>0 update fo set fo.nome=inserted.nome from inserted,fo2 where fo2.fo2stamp=fo.fostamp and fo.no=inserted.no and fo.estab=inserted.estab and inserted.forvd=0 and fo2.assinatura='' IF (select count(*) from po inner join inserted on po.no=inserted.no and po.estab=inserted.estab and inserted.forvd=0 )>0 update po set po.nome=inserted.nome from inserted where po.no=inserted.no and po.estab=inserted.estab and inserted.forvd=0 END SET Context_Info 0x22222 IF (select count(*) from inserted inner join deleted on inserted.flstamp=deleted.flstamp where inserted.nome2<>deleted.nome2 )>0 BEGINIF (select count(*) from fo inner join inserted on fo.no=inserted.no and fo.estab=inserted.estab and inserted.forvd=0 )>0 update fo set fo.nome2=inserted.nome2 from inserted,fo2 where fo2.fo2stamp=fo.fostamp and fo.no=inserted.no and fo.estab=inserted.estab and inserted.forvd=0 and fo2.assinatura='' IF (select count(*) from po inner join inserted on po.no=inserted.no and po.estab=inserted.estab and inserted.forvd=0 )>0 update po set po.nome2=inserted.nome2 from inserted where po.no=inserted.no and po.estab=inserted.estab and inserted.forvd=0 ENDIF (select count(*) from inserted inner join deleted on inserted.flstamp=deleted.flstamp where inserted.tipo<>deleted.tipo )>0 BEGINIF (select count(*) from fc inner join inserted on fc.no=inserted.no and fc.estab=inserted.estab and inserted.forvd=0 )>0 update fc set fc.tipo=inserted.tipo from inserted where fc.no=inserted.no and fc.estab=inserted.estab and inserted.forvd=0 IF (select count(*) from fo inner join inserted on fo.no=inserted.no and fo.estab=inserted.estab and inserted.forvd=0 )>0 update fo set fo.tipo=inserted.tipo from inserted where fo.no=inserted.no and fo.estab=inserted.estab and inserted.forvd=0 IF (select count(*) from po inner join inserted on po.no=inserted.no and po.estab=inserted.estab and inserted.forvd=0 )>0 update po set po.tipo=inserted.tipo from inserted where po.no=inserted.no and po.estab=inserted.estab and inserted.forvd=0 END SET Context_Info 0x If (select count(*) from inserted inner join deleted on inserted.flstamp=deleted.flstamp where inserted.vencimento<>deleted.vencimento and inserted.estab=0 )>0 BEGINIf (select count(*) from fl inner join inserted on fl.no=inserted.no and fl.estab<>0 and inserted.estab=0 )>0 update fl set fl.vencimento=inserted.vencimento from inserted where fl.no=inserted.no and fl.estab<>0 and inserted.estab=0 ENDENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_us_update ON US WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON 
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.tpe') AND type in (N'U'))
if (select count(*) from inserted,deleted where RTRIM(inserted.ugstamp)<>RTRIM(deleted.ugstamp) ) > 0 
BEGIN 
delete from tme FROM deleted
where (RTRIM(tme.trestamp) + RTRIM(cast(tme.usno as varchar(10))) in ( 
select distinct RTRIM(tpe.trestamp) + RTRIM(cast(deleted.userno as varchar(10))) from deleted
left join pfg on deleted.ugstamp = pfg.ugstamp 
inner join tpe on pfg.pfstamp=tpe.pfstamp 
inner join tme on tme.usno = deleted.userno and tme.trestamp=tpe.trestamp 
where deleted.usstamp not in (  
select distinct us.usstamp from pfu 
inner join us on pfu.usstamp=us.usstamp  
where pfu.pfstamp=tpe.pfstamp ))) 
END    
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.tpe') AND type in (N'U'))
if (select SUM(case when RTRIM(inserted.ugstamp) = '' or  inserted.ugstamp is null then 0 else 1 end) from inserted) > 0 
BEGIN 
INSERT INTO TME(tmestamp,tred,trestamp,pfstamp,usno,usna,eadmin,nsegue,ousrinis,ousrdata, ousrhora,usrinis,usrdata,usrhora,marcada,prefe,nread) 
select substring(cast(newid() as varchar(40)),0,25) as 'tmestamp', * from (
select distinct tpe.tred, tpe.trestamp, tpe.pfstamp, inserted.userno, inserted.username, '0' as 'eadmin', '0' as 'nsegue', inserted.ousrinis, inserted.ousrdata, inserted.ousrhora,  
inserted.usrinis, inserted.usrdata, inserted.usrhora, '0' as 'marcada', '0' as 'prefe', '0' as 'nread' from inserted
inner join pfg on inserted.ugstamp = pfg.ugstamp 
inner join tpe on pfg.pfstamp = tpe.pfstamp 
where inserted.usstamp not in(select us.usstamp from tme inner join us on tme.usno = us.userno where tme.trestamp = tpe.trestamp) ) us 
END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_faca_update ON faca WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE FAC SET fac.adiantado=fac.adiantado-(select sum(deleted.valor) from deleted where fac.facstamp=deleted.facstamp),fac.eadiantado=fac.eadiantado-(select sum(deleted.evalor) from deleted where fac.facstamp=deleted.facstamp) from deleted where fac.facstamp=deleted.facstampDELETE FROM OL from deleted where ol.facastamp=deleted.facastamp UPDATE FAC SET fac.adiantado=fac.adiantado+(select sum(inserted.valor) from inserted where fac.facstamp=inserted.facstamp),fac.eadiantado=fac.eadiantado+(select sum(inserted.evalor) from inserted where fac.facstamp=inserted.facstamp) from inserted where fac.facstamp=inserted.facstampif (select count(*) from inserted where inserted.valor<>0 or inserted.evalor<>0)>0INSERT INTO OL (olstamp,facastamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,moeda,descricao,olcodigo,grupo,sgrupo,ollocal,contado,local) select inserted.facastamp,inserted.facastamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'FACA',inserted.cdesc,convert(char(15),inserted.nrdoc),inserted.data,inserted.data,inserted.valor-inserted.juros,inserted.evalor-inserted.ejuros,'PTE ou EURO',inserted.obs,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.ollocal,inserted.contado,inserted.telocal from inserted inner join oc on oc.olcodigo=inserted.olcodigo where inserted.valor<>0 or inserted.evalor<>0
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vr_update ON vr WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE from OL from deleted where ol.vrstamp = deleted.vrstamp and deleted.vrstamp not in (select inserted.vrstamp from inserted where inserted.lol=1)INSERT INTO OL (olstamp,vrstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,intid,sgrupo,grupo,ollocal,olcodigo,moeda,saidm )select inserted.vrstamp,inserted.vrstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'VR',inserted.cheque,(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),inserted.ccusto,inserted.ncusto,inserted.oldata,inserted.data,inserted.fref,'Pagamento a '+inserted.nome,inserted.vfin,inserted.evfin,inserted.contado,inserted.telocal,inserted.intid,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,inserted.moeda,inserted.mvfin from inserted where inserted.lol=1 and inserted.vrstamp not IN (select deleted.vrstamp from deleted where deleted.lol=1)UPDATE OL SETol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.ousrinis=inserted.ousrinis,ol.ousrhora=inserted.ousrhora,ol.ousrdata=inserted.ousrdata,ol.origem='VR',ol.cheque=inserted.cheque,ol.documento=(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.data=inserted.oldata,ol.dvalor=inserted.oldata,ol.fref=inserted.fref,ol.descricao='Pagamento a '+inserted.nome,ol.said=inserted.vfin,ol.esaid=inserted.evfin,ol.contado=inserted.contado,ol.local=inserted.telocal,ol.intid=inserted.intid,ol.sgrupo=inserted.sgrupo,ol.grupo=inserted.grupo,ol.ollocal=inserted.ollocal,ol.olcodigo=inserted.olcodigo,ol.moeda=inserted.moeda,ol.saidm=inserted.mvfin from inserted where inserted.lol=1 and inserted.vrstamp=ol.vrstamp and inserted.vrstamp IN (select deleted.vrstamp from deleted where deleted.lol=1)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cm3_update ON CM3 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (cmdesc) BEGIN update cl set cl.vendnm=inserted.cmdesc from inserted where cl.vendedor=inserted.cm END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fopd_delete ON fopd WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE FC SET fc.difcambio=fc.difcambio-(SELECT SUM(deleted.difcambio) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.edifcambio=fc.edifcambio-(SELECT SUM(deleted.edifcambio) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.debf=fc.debf-(SELECT SUM(deleted.vreg - deleted.virs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.edebf=fc.edebf-(SELECT SUM(deleted.evreg - deleted.evirs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.debfm=fc.debfm-(SELECT SUM(deleted.vmreg) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.virsreg = fc.virsreg - (SELECT SUM(deleted.virs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.evirsreg = fc.evirsreg - (SELECT SUM(deleted.evirs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp) from fc inner join deleted on deleted.fcstamp = fc.fcstamp where deleted.fcstamp <> '' update FC set fc.credf=fc.credf-(SELECT SUM(deleted.vreg) from deleted where  deleted.fostamp <> '' and deleted.fostamp = fc.fostamp), fc.ecredf=fc.ecredf-(SELECT SUM(deleted.evreg) from deleted where  deleted.fostamp <> '' and deleted.fostamp = fc.fostamp), fc.credfm=fc.credfm-(SELECT SUM(deleted.vmreg) from deleted where  deleted.fostamp <> '' and deleted.fostamp = fc.fostamp) from fc inner join deleted on deleted.fostamp = fc.fostamp inner join pd on deleted.pdstamp = pd.pdstamp where deleted.fostamp <> '' update pd set pd.integrado=(case isnull((select top 1 fopd.fopdstamp from fopd where fopd.pdstamp=deleted.pdstamp),'') when '' then (case ISNULL((select top 1 pl.fcstamp from pl inner join fc on pl.fcstamp=fc.fcstamp where fc.pdstamp=deleted.pdstamp and fc.origem='PD'),'') when '' then 0 else 1 end) else 1 end) from deleted where pd.pdstamp=deleted.pdstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tli_insert ON tli WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_wwf_update ON wwf WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nid) BEGIN UPDATE wwfa SET wwfa.wwfnid=inserted.nid from INSERTED where wwfa.wwfstamp=inserted.wwfstamp UPDATE wwfl SET wwfl.wwfnid=inserted.nid from INSERTED where wwfl.wwfstamp=inserted.wwfstamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mt_update ON MT WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONupdate va set va.kmsatehoje=isnull((select max(kms) from it (nolock) where it.matricula=va.matricula)-va.kmsinicial,0)update va set va.kmsatehoje=isnull((select max(kms) from mt (nolock) where mt.matricula=va.matricula and mt.realizada=1 and mt.kms>va.kmsatehoje+va.kmsinicial)-va.kmsinicial,va.kmsatehoje)update va set va.ultimokm=va.kmsatehoje+va.kmsinicialupdate va set va.kmsatehoje=inserted.kms-va.kmsinicial, va.ultimokm=inserted.kms from inserted where inserted.matricula=va.matricula and inserted.kms>va.ultimokm and inserted.realizada=1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ll_delete ON ll WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE FC SET fc.credf=fc.credf-case when deleted.rec>0 then deleted.rec-deleted.arred else 0 end from deleted where deleted.fcstamp=fc.fcstampupdate fc set fc.debf=fc.debf+case when deleted.rec<0 then deleted.rec-deleted.arred else 0 end from deleted where deleted.fcstamp=fc.fcstampupdate fc set fc.ecredf=fc.ecredf-case when deleted.erec>0 then deleted.erec-deleted.earred else 0 end from deleted where deleted.fcstamp=fc.fcstampupdate fc set fc.edebf=fc.edebf+case when deleted.erec<0 then deleted.erec-deleted.earred else 0 end from deleted where deleted.fcstamp=fc.fcstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pp_update ON pp WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(descricao) update pc set pc.ppdesc=inserted.descricao from inserted where pc.ppconta=inserted.conta and pc.ano=inserted.ano GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_le_delete ON le WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM CC from deleted where cc.lestamp=deleted.lestampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fo_delete ON fo WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate FL set
fl.acmfact =fl.acmfact-deleted.ttiliq
, fl.eacmfact =fl.eacmfact-deleted.ettiliq
from DELETED
inner join fo2 on fo2.fo2stamp =deleted.fostamp
where fl.no= deleted.no and fo2.anulado =0

delete from FC from deleted where fc.fostamp =deleted.fostamp
delete from OL from deleted where ol.fostamp =deleted.fostamp
delete from fofc from deleted where fofc.fostamp =deleted.fostampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fo2_update ON fo2 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF update(anulado)
BEGIN
UPDATE FL SET
fl.acmfact=fl.acmfact-fo.ttiliq,
fl.eacmfact=fl.eacmfact-fo.ettiliq
from inserted i
inner join deleted d on d.fo2stamp=i.fo2stamp
inner join fo on fo.fostamp=i.fo2stamp
where fl.no=fo.no and d.anulado<>i.anulado
END
IF update(anulado)
BEGIN
UPDATE fo2 SET
fo2.nmdocfo = '',
fo2.fnofo = 0,
fo2.adocfo = '',
fo2.facturada = 0
from inserted
inner join deleted on deleted.fo2stamp=inserted.fo2stamp
inner join fn i on inserted.fo2stamp = i.fostamp
inner join fo on
fo.doccode = i.ndocfo and fo.no = i.fnofo and fo.foano =  i.foanofo and fo.adoc = i.adocfo
where inserted.anulado=1 and deleted.anulado = 0
and fo2.fo2stamp in (select fo2.fo2stamp from fo2
inner join fo on fo.fostamp = fo2.fo2stamp
where fo.doccode = i.ndocfo and fo.no = i.fnofo and fo.foano = i.foanofo and fo.adoc = i.adocfo)
UPDATE FO2 SET FO2.arquivadodigital = 0
FROM inserted
INNER JOIN deleted ON deleted.FO2stamp=inserted.FO2stamp
INNER JOIN FO2 ON FO2.FO2stamp=inserted.FO2stamp
WHERE inserted.anulado=1 AND deleted.anulado=0
END
IF update(anulado)
BEGIN
UPDATE fn SET
fn.adocfo = '',
fn.fnofo = 0,
fn.foanofo = 0,
fn.facturada = 0
from inserted
inner join deleted on deleted.fo2stamp=inserted.fo2stamp
inner join fn on fn.fostamp = inserted.fo2stamp
where inserted.anulado=1 and deleted.anulado = 0
END
IF (update(ttinvsuj) or  update(ettinvsuj))
BEGIN
UPDATE FO
SET FO.tpdesc=FO.tpdesc
from inserted i
inner join fo on fo.fostamp=i.fo2stamp
END		 
IF update(cheque)
BEGIN 
UPDATE chf
SET deststamp = '' 
, destino = ''
from deleted 
inner join chf on deleted.chfstamp = chf.chfstamp
UPDATE chf
SET deststamp = inserted.fo2stamp 
, destino =  'FO' 
, nome = case when chf.nome = '' then fo.nome else chf.nome end 
, no = case when chf.no = 0 then fo.no else chf.no end 
, estab = case when chf.no = 0 then fo.estab else chf.estab end 					
from inserted  
inner join chf on inserted.chfstamp = chf.chfstamp
inner join fo on fo.fostamp = inserted.fo2stamp
END 		GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fofc_update ON fofc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE FC set fc.obs=inserted.obs,fc.usrdata=fo.usrdata,fc.usrhora=fo.usrhora,fc.usrinis=fo.usrinis,fc.tpdesc=fo.tpdesc,fc.tpstamp=fo.tpstamp,fc.datalc=fo.data,fc.no=fo.no,fc.nome=fo.nome,fc.cm=cm1.fomulti,fc.cmdesc=cm1.fomultidesc,fc.cred=case when cm1.debito=1 then 0 else inserted.cred end,fc.ecred=case when cm1.debito=1 then 0 else inserted.ecred end,fc.deb=case when cm1.debito=1 then inserted.cred else 0 end,fc.edeb=case when cm1.debito=1 then inserted.ecred else 0 end,fc.credm=case when cm1.debito=1 then 0 else inserted.credm end,fc.debm=case when cm1.debito=1 then inserted.credm else 0 end,fc.aprovado=fo.aprovado,fc.nmaprov=fo.nmaprov,fc.dtaprov=fo.dtaprov,fc.ivav1=inserted.ivav1,fc.ivav2=inserted.ivav2,fc.ivav3=inserted.ivav3,fc.ivav4=inserted.ivav4,fc.ivav5=inserted.ivav5,fc.ivav6=inserted.ivav6,fc.ivav7=inserted.ivav7,fc.ivav8=inserted.ivav8,fc.ivav9=inserted.ivav9, fc.eivav1=inserted.eivav1,fc.eivav2=inserted.eivav2,fc.eivav3=inserted.eivav3,fc.eivav4=inserted.eivav4,fc.eivav5=inserted.eivav5,fc.eivav6=inserted.eivav6,fc.eivav7=inserted.eivav7,fc.eivav8=inserted.eivav8,fc.eivav9=inserted.eivav9, fc.moeda=fo.moeda,fc.adoc=fo.adoc,fc.dataven=inserted.dataven,fc.fref=fo.fref,fc.cambiofixo=fo.cambiofixo,fc.ccusto=fo.ccusto,fc.ncusto=fo.ncusto,fc.intid=fo.intid,fc.tipo=fo.tipo,fc.estab=fo.estab,fc.pais=fo.pais,fc.zona=fo.zona,fc.formapag=inserted.formapag,fc.virs=inserted.virs,fc.evirs=inserted.evirs from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.fofcstamp=fc.fofcstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vl_delete ON vl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE VY SET vy.valornp=vy.valornp+deleted.rec,vy.evalornp=vy.evalornp+deleted.erec from deleted where deleted.vystamp=vy.vystamp and deleted.process=1 UPDATE VY SET vy.ultdoc='',vy.recibado=0 from deleted where vy.vystamp=deleted.vystamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tb_delete ON tb WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.tbstamp=deleted.tbstampUPDATE PR SETpr.tbcheque='',pr.tbstamp='',pr.tbok=0from deleted where pr.tbstamp=deleted.tbstampUPDATE FR SETfr.tbcheque='',fr.tbstamp='',fr.tbok=0from deleted where fr.tbstamp=deleted.tbstampUPDATE VR SETvr.tbcheque='',vr.tbstamp='',vr.tbok=0from deleted where vr.tbstamp=deleted.tbstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chf_delete ON CHF WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted inner join ol on deleted.chfstamp = ol.oristamp WHERE ol.origem = 'CHF'  

DELETE FROM od FROM deleted WHERE od.oristamp = deleted.chfstamp AND od.origem = 'CHF'

IF exists(select 1 from deleted where deleted.origem = 'CHF' AND deleted.oristamp<>'')
BEGIN 
DELETE FROM chfl 
FROM chfl inner join deleted on chfl.chfstamp = deleted.oristamp
WHERE chfl.cm = 4 

UPDATE chf
SET deststamp = deleted.deststamp
, destino = deleted.destino
from chf inner join deleted on chf.chfstamp = deleted.oristamp 

END			GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pe_delete ON PE WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete from pecc from deleted where pecc.pestamp=deleted.pestampdelete from fd from deleted where fd.no=deleted.no
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tli_update ON tli WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftrd_delete ON ftrd WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from CC from deleted where cc.ccstamp='C'+deleted.ftrdstamp or cc.ccstamp='D'+deleted.ftrdstamp UPDATE CC SET cc.difcambio = cc.difcambio - ISNULL((select SUM(deleted.difcambio) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), 0), cc.edifcambio = cc.edifcambio - ISNULL((select SUM(deleted.edifcambio) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), 0), cc.credf=cc.credf - (select SUM(deleted.vreg) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), cc.ecredf=cc.ecredf - (select SUM(deleted.evreg) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), cc.credfm=cc.credfm - (select SUM(deleted.vmreg) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp)  from cc inner join deleted on deleted.ccstamp=cc.ccstamp where deleted.ccstamp <> '' update cc set cc.debf=cc.debf - (select SUM(deleted.vreg) from deleted where deleted.ccstamp <> '' and deleted.ftstamp = cc.ftstamp and deleted.multi = 0), cc.edebf=cc.edebf - (select SUM(deleted.evreg) from deleted where deleted.ccstamp <> '' and deleted.ftstamp = cc.ftstamp and deleted.multi = 0), cc.debfm=cc.debfm - (select SUM(deleted.vmreg) from deleted where deleted.ccstamp <> '' and deleted.ftstamp = cc.ftstamp and deleted.multi = 0) from cc inner join ft on ft.ftstamp = cc.ftstamp inner join deleted on deleted.ftstamp=cc.ftstamp inner join td on td.ndoc = ft.ndoc where ft.anulado = 0 and cc.ccstamp<>'R'+deleted.ftstamp and ft.multi = 0 and deleted.multi = 0 and deleted.ccstamp <> '' and td.ccautoreg = 0 update rd set rd.integrado=(case isnull((select top 1 ftrd.ftrdstamp from ftrd where ftrd.rdstamp=deleted.rdstamp),'') when '' then 0 else 1 end) from deleted where rd.rdstamp=deleted.rdstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftreg_delete ON ftreg WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdeclare @m_debfm numeric(19,6)declare @m_ftregstamp varchar(25)select @m_ftregstamp=(select top 1 deleted.ftregstamp from deleted group by deleted.ftregstamp)select @m_debfm=(select sum(ftreg.vmoeda) from ftreg where ftreg.ftregstamp=@m_ftregstamp)UPDATE CC set cc.debf=(case when cc.debf-round(deleted.vciva,0)<0 then 0 else cc.debf-round(deleted.vciva,0) end) , cc.edebf=(case when cc.edebf-round(deleted.evciva,2 )<0 then 0 else cc.edebf-round(deleted.evciva,2 ) end), cc.debfm=(case when @m_debfm<0 then 0 else @m_debfm end), cc.ultdoc='' from deleted where cc.ftstamp=deleted.ftregstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_gsec_update ON GSEC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (select count(*) from inserted inner join deleted on inserted.gsecstamp=deleted.gsecstamp where inserted.gsenome<>deleted.gsenome)>0 beginupdate fl set fl.gsenome=inserted.gsenome from inserted where fl.gsecstamp=inserted.gsecstamp endGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tmr_insert ON tmr WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET tpo.etentr=tpo.etentr+inserted.eentr, tpo.etsaid=tpo.etsaid+inserted.esaid, tpo.tentr=tpo.tentr+inserted.entr, tpo.tsaid=tpo.tsaid+inserted.said, TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where tpo.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tmr_delete ON tmr WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE TPO SET tpo.etentr=tpo.etentr-deleted.eentr, tpo.etsaid=tpo.etsaid-deleted.esaid, tpo.tentr=tpo.tentr-deleted.entr, tpo.tsaid=tpo.tsaid-deleted.said from deleted where tpo.tpostamp=deleted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_le_insert ON le WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO CC (ccstamp,lestamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,estab,nome,cm,cmdesc,origem,cred,ecred,credf,ecredf,nrdoc,dataven,fref,intid,ccusto,ncusto,pais,obs,zona) select inserted.lestamp,inserted.lestamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.data,inserted.no,inserted.estab,inserted.nome,80,(select cmdesc from cm1 (nolock) where cm=80),'LE',inserted.totalcc-inserted.arred,inserted.etotalcc-inserted.earred,inserted.totalcc-inserted.arred,inserted.etotalcc-inserted.earred,inserted.leno,inserted.dataven,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.pais,ltrim(inserted.ccdesc),cl.zona from inserted inner join cl on inserted.no=cl.no and inserted.estab=cl.estab where inserted.totalcc<>0 or inserted.etotalcc<>0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mgdisk_update ON mgdisk WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON	if exists (select * from inserted where inserted.mapaprinc = 1) 
					and update(mapaprinc)
	begin 
		
		declare @hora as varchar(10)
		set @hora = (select convert(varchar(10), GETDATE(), 108))
		insert into mod (modstamp, origem, tiporigem,  grupo, subgrupo,   
			resumo, descricao, mapabase, confbase, tipomapa, 
			oristamp, codmapa, usrdata, usrinis, usrhora, 
			ousrdata, ousrinis, ousrhora )
		select substring(cast(newid() as char(36)),1,25) 
			, 'Definida pelo utilizador'
			, 'Mapas de Gestão em Suporte Informático' 
			, 'Contabilidade', inserted.grupo
			, inserted.titulo, inserted.mdesc 
			, inserted.titulo, 1, '4MSI', inserted.idustamp, 'MSI'
			, getdate(), 'ADM', @hora
			, getdate(), 'ADM', @hora
		from inserted
		where inserted.mapaprinc = 1 
	
	end 
	
	else if exists (select * from inserted where inserted.mapaprinc = 0) 
					and update(mapaprinc)
	begin 
	
		delete from mod 
		from inserted
		where mod.oristamp = inserted.idustamp	
		
	end 
	GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mxdv_update ON MXDV WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(div) update mx set mx.div=inserted.div from inserted where mx.coddiv=inserted.coddiv GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ol_delete ON ol WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM BA FROM deleted where ba.olstamp=deleted.olstampIF (select count(*) from deleted where deleted.local='C')>0 and not EXISTS(select fe.data from fe,deleted where deleted.local='C' and convert(char,fe.data,112)=convert(char,deleted.data,112))INSERT INTO FE (festamp,data,saldof,esaldof)          select data as festamp,                data as data,                Isnull((select top 1 Isnull(saldof,0) from fe where convert(char,fe.data,112)<convert(char,deleted.data,112) order by fe.data desc),0) as saldof,                Isnull((select top 1 Isnull(esaldof,0) from fe where convert(char,fe.data,112)<convert(char,deleted.data,112) order by fe.data desc),0) as esaldof          from deleted where deleted.local='C' and not EXISTS(select fe.data from fe,deleted where deleted.local='C' and convert(char,fe.data,112)=convert(char,deleted.data,112)) group by data update fe set fe.saldof=fe.saldof-(select sum(entr-said) from deleted where deleted.local='C'),fe.esaldof=fe.esaldof-(select sum(eentr-esaid) from deleted where deleted.local='C') from fe,deleted where deleted.local='C' and fe.data>=convert(char,deleted.data,112) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ox_update ON ox WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE OL set ol.transf=1,ol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.dvalor=inserted.dvalor,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.data=inserted.data,ol.descricao=inserted.oxdesc,ol.entr=inserted.valor,ol.eentr=inserted.evalor,ol.entrm=inserted.valorm,ol.moeda=inserted.moeda,ol.cheque=inserted.notalaofrom inserted where inserted.contado>0 and ol.olstamp=inserted.oxstampUPDATE OL set ol.transf=1,ol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.dvalor=inserted.dvalor,ol.data=inserted.data,ol.ollocal=inserted.olorigem,ol.contado=inserted.ocontado,ol.local=case when inserted.ocontado>0 then 'B' else 'C' end,ol.descricao=inserted.oxdesc,ol.said=inserted.valor,ol.esaid=inserted.evalor,ol.saidm=inserted.valorm1,ol.moeda=inserted.moeda1,ol.cheque=inserted.notalaofrom inserted where inserted.contado>0 and ol.olstamp=substring('C'+inserted.oxstamp,1,25)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_nabdr_update ON nabdr WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON			IF update(principal) AND (select COUNT(*)
									  from inserted
									  where principal=1)=1
				UPDATE nabdr
					set principal=0
				from inserted i
				where nabdr.nabdrstamp<>i.nabdrstamp
					and nabdr.ano=i.ano
					and i.principal=1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cm1_update ON CM1 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (cmdesc) BEGIN IF UPDATE (cmdesc)BEGINupdate cc set cc.cmdesc=inserted.cmdesc from inserted where cc.cm = inserted.cmupdate fc set fc.cmdesc=inserted.cmdesc from inserted where fc.cm = inserted.cmupdate td set td.cmccn=inserted.cmdesc from inserted where td.cmcc = inserted.cmupdate po set po.cmdesc=inserted.cmdesc from inserted where po.cm = inserted.cmupdate fo set fo.docnome=inserted.cmdesc from inserted where fo.doccode = inserted.cm update WKDOCS set wkdocs.wkldocnome = inserted.cmdesc  from inserted, wkdocs, deleted   where wkdocs.wkldocnome=deleted.cmdesc and  wkdocs.wkldocno =deleted.cm  update WK set wk.wkdocdescr = inserted.cmdesc    from inserted, wk, deleted  where wk.wkdocdescr=deleted.cmdesc  and wk.wkdoc= deleted.cm END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cpt_update ON cpt WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON			if update(nome)
			begin
				update cptng set
					cptng.nomecpt=i.nome
				from inserted as i
				where cptng.cptstamp=i.cptstamp

				update cptclp set
					cptclp.nomecpt=i.nome
				from inserted as i
				where cptclp.cptstamp=i.cptstamp

				update cptst set
					cptst.nomecpt=i.nome
				from inserted as i
				where cptst.cptstamp=i.cptstamp
			end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_it_update ON IT WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONupdate va set va.custototal=va.custototal-deleted.tt, va.ecustotota=va.ecustotota-deleted.ett from deleted where va.matricula=deleted.matriculaupdate va set va.kmsatehoje=isnull((select max(kms) from it (nolock) where it.matricula=va.matricula)-va.kmsinicial,0)update va set va.kmsatehoje=isnull((select max(kms) from mt (nolock) where mt.matricula=va.matricula and mt.realizada=1 and mt.kms>va.kmsatehoje+va.kmsinicial)-va.kmsinicial,va.kmsatehoje)update va set va.ultimokm=va.kmsatehoje+va.kmsinicial, va.ultimadata=Isnull((select Max(it.data) from it where it.matricula=va.matricula),space(8))update vc set vc.custototal=vc.custototal-deleted.tt, vc.ecustotota=vc.ecustotota-deleted.ett, vc.ultimadata=Isnull((select Max(it.data) from it where it.idacess=deleted.idacess),space(8)) from deleted where vc.idacess=deleted.idacessupdate va set va.custototal=va.custototal+inserted.tt, va.ecustotota=va.ecustotota+inserted.ett from inserted where va.matricula=inserted.matriculaupdate va set va.kmsatehoje=inserted.kms-va.kmsinicial, va.ultimokm=inserted.kms, va.ultimadata=inserted.data from inserted where inserted.matricula=va.matricula and inserted.kms>va.ultimokmupdate vc set vc.matricula=inserted.matricula, vc.custototal=vc.custototal+inserted.tt, vc.ecustotota=vc.ecustotota+inserted.ett, vc.ultimadata=case when vc.ultimadata<inserted.data then inserted.data else vc.ultimadata end from inserted where vc.idacess=inserted.idacess
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_it_delete ON IT WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate va set va.custototal=va.custototal-deleted.tt, va.ecustotota=va.ecustotota-deleted.ett from deleted where va.matricula=deleted.matriculaupdate va set va.kmsatehoje=isnull((select max(kms) from it (nolock) where it.matricula=va.matricula)-va.kmsinicial,0)update va set va.kmsatehoje=isnull((select max(kms) from mt (nolock) where mt.matricula=va.matricula and mt.realizada=1 and mt.kms>va.kmsatehoje+va.kmsinicial)-va.kmsinicial,va.kmsatehoje)update va set va.ultimokm=va.kmsatehoje+va.kmsinicial, va.ultimadata=Isnull((select Max(it.data) from it where it.matricula=va.matricula),space(8))update vc set vc.custototal=vc.custototal-deleted.tt, vc.ecustotota=vc.ecustotota-deleted.ett, vc.ultimadata=Isnull((select Max(it.data) from it where it.idacess=deleted.idacess),space(8)) from deleted where vc.idacess=deleted.idacess
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ccfcl_delete ON ccfcl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate re
SET re.anulado=1,
anuldata=CONVERT(VARCHAR(8),GETDATE(),112),
anulinis=SYSTEM_USER,
anulhora=CONVERT(VARCHAR(5),GETDATE(),114)
from re inner join deleted on re.restamp = deleted.restamp 	DELETE FROM po from deleted where po.postamp=deleted.postamp DELETE FROM pl from deleted where pl.plstamp=deleted.plstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_MGE1MDC_insert ON MGE1MDC WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON	
	declare @hora as varchar(10)
	set @hora = (select convert(varchar(10), GETDATE(), 108))
	insert into mod (modstamp, origem, tiporigem, grupo, subgrupo, 
			resumo, descricao, mapabase, confbase, tipomapa, 
			oristamp, codmapa, usrdata, usrinis, usrhora, 
			ousrdata, ousrinis, ousrhora, url)
	select substring(cast(newid() as char(36)),1,25) 
			, 'Definida pelo utilizador'
			, 'Mapas de Gestão', 'Contabilidade'
			, inserted.grupo, inserted.titulo, mge1mdc.mdesc 
			, inserted.titulo, case when isnull(inserted.mapaphc, 0) = 0 then 0 else 1 end 
			, '4MG', inserted.idustamp, 'MG'
			, getdate(), 'ADM', @hora
			, getdate(), 'ADM', @hora
			, inserted.urlmg
	from inserted
	inner join mge1mdc on mge1mdc.mge1mdcstamp = inserted.mge1mdcstamp
	where inserted.odeclarativas = 1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_MGE1MDC_update ON MGE1MDC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON	if exists (select * from inserted where inserted.odeclarativas = 1)
					and update(odeclarativas)
	begin
		
		declare @hora as varchar(10)
		set @hora = (select convert(varchar(10), GETDATE(), 108))
		insert into mod (modstamp, origem, tiporigem, grupo, subgrupo,  
			resumo, descricao, mapabase, confbase, tipomapa, 
			oristamp, codmapa, usrdata, usrinis, usrhora, 
			ousrdata, ousrinis, ousrhora, url)
		select substring(cast(newid() as char(36)),1,25) 
			, 'Definida pelo utilizador'
			, 'Mapas de Gestão', 'Contabilidade'
			, inserted.grupo, inserted.titulo, mge1mdc.mdesc
			, inserted.titulo, case when isnull(inserted.mapaphc, 0) = 0 then 0 else 1 end 
			, '4MG', inserted.idustamp, 'MG'
			, getdate(), 'ADM', @hora
			, getdate(), 'ADM', @hora
			, inserted.urlmg
		from inserted
		inner join mge1mdc on mge1mdc.mge1mdcstamp = inserted.mge1mdcstamp
		where inserted.odeclarativas = 1
	end 
	
	else if exists (select * from inserted where inserted.odeclarativas = 0)
					and update(odeclarativas)
	begin 
	
		delete from mod 
		from inserted
		where mod.oristamp = inserted.idustamp	
		
	end
	GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fmce_update ON FMCE WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (descce) BEGIN if update(descce) BEGINupdate fmec set fmec.descce=inserted.descce from inserted where fmec.codce=inserted.codce END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fm_update ON FM WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (datini) or update(datfim) or update(horaini) or update(horafim) BEGIN if update(datini) BEGINupdate fmin set fmin.datini=inserted.datini from inserted where fmin.fmstamp=inserted.fmstamp ENDif update(datfim) BEGINupdate fmin set fmin.datfim=inserted.datfim from inserted where fmin.fmstamp=inserted.fmstamp ENDif update(horaini) BEGINupdate fmin set fmin.horaini=inserted.horaini from inserted where fmin.fmstamp=inserted.fmstamp ENDif update(horafim) BEGINupdate fmin set fmin.horafim=inserted.horafim from inserted where fmin.fmstamp=inserted.fmstamp END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fi2_insert ON fi2 WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF exists (select 1 from inserted where inserted.oristamp <> '' AND inserted.origem='CHL') 
BEGIN
UPDATE CHL 
SET chl.FTSTAMP = inserted.ftstamp
from chl 
inner join inserted on chl.chlstamp=inserted.oristamp and inserted.origem='CHL'
ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ag_delete ON AG WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate cl set cl.agno=0,cl.eag=0 from deleted where cl.agno=deleted.noupdate fl set fl.agno=0,fl.eag=0 from deleted where fl.agno=deleted.noGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sftnome_update ON sftnome WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE SFTAM set sftam.nome=inserted.campofrom deleted inner join inserted on inserted.sftnomestamp =deleted.sftnomestamp  where sftam.nome=deleted.campoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_caltl_delete ON caltl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from calp from deleted inner join caltc on deleted.caltcstamp = caltc.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where calp.caltlstamp = deleted.caltlstamp and calp.fechado = 0 and calp.calstamp = cal.calstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mgdisk_insert ON mgdisk WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON	declare @hora as varchar(10)
	set @hora = (select convert(varchar(10), GETDATE(), 108))
	insert into mod (modstamp, origem, tiporigem, grupo,  subgrupo, 
			resumo, descricao, mapabase, confbase, tipomapa, 
			oristamp, codmapa, usrdata, usrinis, usrhora, 
			ousrdata, ousrinis, ousrhora )
	select  substring(cast(newid() as char(36)),1,25) 
			, 'Definida pelo utilizador'
			, 'Mapas de Gestão em Suporte Informático'
			, 'Contabilidade', inserted.grupo
			, inserted.titulo, inserted.mdesc 
			, inserted.titulo, 1, '4MSI', inserted.idustamp, 'MSI'
			, getdate(), 'ADM', @hora
			, getdate(), 'ADM', @hora
	from inserted
	where inserted.mapaprinc = 1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_clp_update ON clp WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON			if update(nome) or update(nomeempresa)
				update cptclp set
					cptclp.nomeclp=case when i.nomeempresa='' then i.nome else i.nomeempresa end
				from inserted as i
				where cptclp.clpstamp=i.clpstamp
			if update(area)
				update cptclp set
					cptclp.area=i.area
				from inserted as i
				where cptclp.clpstamp=i.clpstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ch_update ON CH WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON				   if (select count(*) from inserted where inserted.choristamp <> '') > 0
				  BEGIN 
				   	UPDATE chl
				   	SET  chl.data = inserted.data, chl.valor=inserted.valor, chl.evalor=inserted.evalor, chl.moeda=inserted.moeda, chl.valorm=inserted.valorm, chl.ollocal=inserted.ollocal, chl.contado=inserted.contado
				   	from chl, inserted
				   	where inserted.chstamp = chl.chlstamp AND chl.cm = 4
				  END	
				  IF (update(evalor) OR update(valor) OR update(clcheque) OR update(valorm) OR update(data) OR update(moeda)) AND  (select count(*) from inserted where inserted.choristamp <> '') = 0
				  BEGIN 
				  	UPDATE chl
				     	SET  chl.data = inserted.data, chl.valor=inserted.valor, chl.evalor=inserted.evalor, chl.moeda=inserted.moeda, chl.valorm=inserted.valorm
				     	from chl, inserted
				     	where inserted.chstamp = chl.chlstamp AND chl.cm = 1
				  END	
				  
				  
				  
				  GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_prre_update ON prre WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE DL SET DL.PROCESS=0,DL.PROCDATA='' from deleted inner join dl on deleted.dlstamp=dl.dlstamp where deleted.dlstamp<>''UPDATE DL SET DL.PROCESS=1,DL.PROCDATA=inserted.DTREF from inserted inner join dl on inserted.dlstamp=dl.dlstamp where inserted.dlstamp<>''
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_gsec_delete ON GSEC WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONif (select count(*) from fl inner join deleted on fl.gsecstamp=deleted.gsecstamp)>0 update fl set fl.gsecstamp='', fl.gsenome=''GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_opt_delete ON opt WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OPD FROM deleted where (opd.poptstamp=deleted.optstamp or opd.foptstamp=deleted.optstamp) and deleted.optstamp<>''DELETE FROM OST FROM deleted where (ost.poptstamp=deleted.optstamp or ost.foptstamp=deleted.optstamp) and deleted.optstamp<>''
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_re_delete ON re WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete from CC from deleted where cc.restamp =deleted.restamp
delete from OL from deleted where ol.restamp =deleted.restampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oy_update ON oy WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE CH SET ch.jadepo=0,ch.dataoy='' from deleted where ch.chstamp=deleted.chstamp  UPDATE ch  SET ch.status =  (select top 1 cm  from ( select chl.cm, chl.ousrdata, chl.ousrhora     from chl INNER JOIN ch ON ch.chstamp = chl.chstamp    inner join deleted on deleted.chstamp = ch.chstamp    union all  select ox.cm, oy.ousrdata, oy.ousrhora  from ox inner join oy on ox.oxstamp = oy.oxstamp  inner join deleted on deleted.chstamp = oy.chstamp	  )t  order by ousrdata+ousrhora desc)   from deleted, ch where deleted.chstamp = ch.chstamp  UPDATE CH set ch.jadepo=1,ch.dataoy=ox.data from inserted inner join ox on ox.oxstamp=inserted.oxstamp where ch.chstamp=inserted.chstamp  UPDATE ch  SET ch.status =  (select top 1 cm  from ( select chl.cm, chl.ousrdata, chl.ousrhora     from chl INNER JOIN ch ON ch.chstamp = chl.chstamp    inner join inserted on inserted.chstamp = ch.chstamp    union all  select ox.cm, oy.ousrdata, oy.ousrhora  from ox inner join oy on ox.oxstamp = oy.oxstamp  inner join inserted on inserted.chstamp = oy.chstamp	  )t  order by ousrdata+ousrhora desc)   from inserted, ch where inserted.chstamp = ch.chstamp  GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fr_insert ON fr WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (olstamp,frstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,intid,sgrupo,grupo,ollocal,olcodigo,moeda,saidm )select inserted.frstamp,inserted.frstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FR',inserted.cheque,(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),inserted.ccusto,inserted.ncusto,inserted.oldata,inserted.data,inserted.fref,'Pagamento a '+inserted.nome,inserted.valor,inserted.evalor,inserted.contado,inserted.telocal,inserted.intid,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,inserted.moeda,inserted.mvalor from inserted where inserted.lol=1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cbb_update ON cbb WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF (select COUNT(*) from inserted i inner join deleted d on d.cbbstamp=i.cbbstamp where d.process<>i.process)>0
UPDATE cbbl set
 naceite=case when d.process=1 and i.process=0 then 0 else cbbl.naceite end
from inserted i
inner join deleted d on d.cbbstamp=i.cbbstamp
inner join cbbl on i.cbbstamp=cbbl.cbbstamp
where d.process<>i.processGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facl_delete ON facl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE CC SET cc.faccstamp='',cc.faclstamp='' from deleted where cc.ccstamp=deleted.ccstampUPDATE FACC SET facc.totalrec=facc.totalrec-(select sum(deleted.escrec) from deleted where facc.facstamp=deleted.facstamp),facc.etotalrec=facc.etotalrec-(select sum(deleted.erec) from deleted where facc.facstamp=deleted.facstamp) from deleted where facc.facstamp=deleted.facstampUPDATE FAC SET fac.cessionado=fac.cessionado-(select sum(deleted.escval-deleted.escrec) from deleted where fac.facstamp=deleted.facstamp),fac.ecessionado=fac.ecessionado-(select sum(deleted.eval-deleted.erec) from deleted where fac.facstamp=deleted.facstamp) from deleted where fac.facstamp=deleted.facstampUPDATE FAL SET fal.cessionado=fal.cessionado-(select sum(deleted.escval-deleted.escrec) from deleted where fal.falstamp=deleted.falstamp),fal.ecessionado=fal.ecessionado-(select sum(deleted.eval-deleted.erec) from deleted where fal.falstamp=deleted.falstamp) from deleted where fal.falstamp=deleted.falstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_acf_update ON ACF WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome) and (select count(*) from plf inner join inserted on plf.acfstamp=inserted.acfstamp)>0 begin update plf set plf.nmacf=inserted.nome from inserted where plf.acfstamp=inserted.acfstamp end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_caltl_update ON caltl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (select count(*) from caltc inner join inserted on inserted.caltcstamp = caltc.caltcstamp where caltc.alteracal = 1) > 0 BEGIN if update(username) or update(userno) UPDATE calp SET calp.username=inserted.username,calp.userno=inserted.userno from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal=1 and calp.caltlstamp=inserted.caltlstamp and calp.fechado = 0 if update(dpt) UPDATE calp SET calp.dpt=inserted.dpt from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal=1 and calp.caltlstamp=inserted.caltlstamp and calp.fechado = 0 if update(tipo) UPDATE calp SET calp.tipo=inserted.tipo from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal=1 and calp.caltlstamp=inserted.caltlstamp and calp.fechado = 0 if update(resumo) UPDATE calp SET calp.resumo=inserted.resumo from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal=1 and calp.caltlstamp=inserted.caltlstamp and calp.fechado = 0 if update(descricao) UPDATE calp SET calp.descricao=inserted.descricao from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal=1 and calp.caltlstamp=inserted.caltlstamp and calp.fechado = 0 if update(ldata) UPDATE calp SET calp.ldata=calp.data + inserted.ldata from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal=1 and calp.caltlstamp=inserted.caltlstamp and calp.fechado = 0 END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_crt_update ON CRT WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (update(nome) or update(tipo)) and (select count(*) from rc3 inner join inserted on rc3.crtstamp=inserted.crtstamp)>0 begin update rc3 set rc3.nmcrt=inserted.nome,rc3.tipo=inserted.tipo from inserted where rc3.crtstamp=inserted.crtstamp endif (update(nome) or update(tipo)) and (select count(*) from cv3 inner join inserted on cv3.crtstamp=inserted.crtstamp)>0 begin update cv3 set cv3.nmcrt=inserted.nome,cv3.tipo=inserted.tipo from inserted where cv3.crtstamp=inserted.crtstamp end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sgc_update ON SGC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE(cor) update sx set sx.cor=inserted.cor from inserted,deleted where sx.ref=inserted.ref and sx.cor=deleted.corGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_relconfig_update ON RELCONFIG WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON		if exists (select 1 from inserted i, deleted d where i.defeito <> d.defeito and i.defeito=1)
		begin
			update relconfig set defeito = 0 
			from inserted i
			where relconfig.idrelcred = i.idrelcred and relconfig.idfornecedor = i.idfornecedor and i.defeito = 1
		endGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_e1li_delete ON e1li WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM E2 FROM deleted where e2.e1listamp=deleted.e1listampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vi_insert ON VI WITH ENCRYPTION FOR INSERT,UPDATE  AS SET NOCOUNT ON IF update (data) or update(hora) or update(lsitu) or update(resumo)  BEGIN update em set em.ultivisita=isnull((select top 1 vi.data  from vi where vi.no=inserted.no and vi.lsitu=1 order by data desc,hora desc),em.ultivisita),em.ultihora=isnull((select top 1 vi.hora  from vi where vi.no=inserted.no and vi.lsitu=1 order by data desc,hora desc),em.ultihora),em.semvis=isnull((select top 1 abs(vi.lsitu-1)  from vi where vi.no=inserted.no and vi.lsitu=1 order by data desc,hora desc),em.semvis),em.proxaccao= isnull((select top 1 vi.resumo from vi where vi.no=inserted.no and vi.lsitu=0  order by data,hora ),''), em.proxvisita=isnull((select top 1 vi.data from vi where vi.no=inserted.no and vi.lsitu=0  order by data,hora ), ''), em.proxhora=isnull((select top 1 vi.hora from vi where vi.no=inserted.no and vi.lsitu=0  order by data,hora ),'') from inserted where em.no=inserted.no  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cev_delete ON cev WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON		DELETE CEVI FROM DELETED WHERE CEVI.cevstamp = (DELETED.cevstamp)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vy_insert ON VY WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OD (vystamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,data,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local) select inserted.vystamp,inserted.vystamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'VY',inserted.demi,inserted.valornp,inserted.evalornp,'Comissões do Vendedor: '+rtrim(inserted.vendnm)+', nº '+convert(char,inserted.vendedor),inserted.olcodigo,inserted.grupo,inserted.sgrupo,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.local from inserted where (inserted.valornp<>0 or inserted.evalornp<>0)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cb_update ON CB WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128) 
SELECT @Cinfo = Context_Info()
IF @Cinfo = 0x12345
RETURN

UPDATE CBPAISES 
SET pais = i.pais,
moeda = i.moeda,
unisg = i.unisg,
unipl = i.unipl,
centsg = i.centsg,
centpl = i.centpl,
zonaeuro = i.zonaeuro
FROM CBPAISES 
INNER JOIN inserted i ON i.pais = cbpaises.pais and i.moeda = cbpaises.moeda
INNER JOIN deleted d on d.cbstamp = i.cbstamp
WHERE i.pais <> d.pais or
i.moeda <> d.moeda or
i.unisg <> d.unisg or 
i.unipl <> d.unipl or
i.centsg <> d.centsg or
i.centpl <> d.centpl or 
i.zonaeuro <> d.zonaeuro

INSERT INTO CBPAISES (pais, moeda, cbpaisesstamp, unisg, unipl, centsg, centpl, zonaeuro)
SELECT pais, moeda, left(replace(cast(newid() as varchar(100)),'-',''),24), unisg, unipl, centsg, centpl, zonaeuro FROM inserted
WHERE NOT EXISTS (select * from cbpaises where cbpaises.pais = inserted.pais and cbpaises.moeda = inserted.moeda)


IF (update(ecambio) OR update(ecambio2)) AND (NOT (update(ecambioinvertido) OR update(ecambioinvertido2)))
BEGIN 
SET Context_Info 0x12345

UPDATE cb
SET cb.ecambioinvertido = 1/CASE WHEN inserted.ecambio<>0 THEN inserted.ecambio ELSE 1 END 
, cb.ecambioinvertido2 = 1/CASE WHEN inserted.ecambio2<>0 THEN inserted.ecambio2 ELSE 1 END 
, cb.cambioinvertido = 1/CASE WHEN inserted.cambio<>0 THEN inserted.cambio ELSE 1 END 
, cb.cambioinvertido2 = 1/CASE WHEN inserted.cambio2<>0 THEN inserted.cambio2 ELSE 1 END 					
FROM cb
INNER JOIN inserted ON cb.cbstamp = inserted.cbstamp	
INNER JOIN deleted ON deleted.cbstamp = inserted.cbstamp
WHERE deleted.ecambio <> inserted.ecambio 
OR deleted.ecambio2 <> inserted.ecambio2

SET Context_Info 0x 		
END

IF (update(ecambioinvertido) OR update(ecambioinvertido2)) AND (NOT (update(ecambio) OR update(ecambio2)))
BEGIN 
SET Context_Info 0x12345

UPDATE cb
SET cb.ecambio = 1/CASE WHEN inserted.ecambioinvertido<>0 THEN inserted.ecambioinvertido ELSE 1 END 
, cb.ecambio2 =  1/CASE WHEN inserted.ecambioinvertido2<>0 THEN inserted.ecambioinvertido2 ELSE 1 END 
, cb.cambio = 1/CASE WHEN inserted.cambioinvertido<>0 THEN inserted.cambioinvertido ELSE 1 END 
, cb.cambio2 =  1/CASE WHEN inserted.cambioinvertido2<>0 THEN inserted.cambioinvertido2 ELSE 1 END 					
FROM cb
INNER JOIN inserted ON cb.cbstamp = inserted.cbstamp
INNER JOIN deleted ON deleted.cbstamp = inserted.cbstamp
WHERE deleted.ecambioinvertido <> inserted.ecambioinvertido 
OR deleted.ecambioinvertido2<> inserted.ecambioinvertido2 


SET Context_Info 0x 		
END

GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ct_update ON ct WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONBEGIN UPDATE MACT SET mact.formula=inserted.formula, mact.nome=inserted.nome from inserted inner join mact on mact.codcount=inserted.codcount UPDATE MMCT SET mmct.formula=inserted.formula, mmct.nome=inserted.nome from inserted inner join mmct on mmct.codcount=inserted.codcount UPDATE MHCT SET mhct.nome=inserted.nome from inserted inner join mhct on mhct.codcount=inserted.codcount UPDATE CSUPCT SET csupct.nome=inserted.nome from inserted inner join csupct on csupct.codcount=inserted.codcount END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_MGE1MDC_delete ON MGE1MDC WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON	if exists (select * from deleted where deleted.odeclarativas = 1)
		delete from mod 
		from deleted
		where mod.oristamp = deleted.idustampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facali_delete ON facali WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE FACA SET faca.valrec=faca.valrec-(select sum(deleted.valrec) from deleted where faca.facastamp=deleted.facastamp),faca.evalrec=faca.evalrec-(select sum(deleted.evalrec) from deleted where faca.facastamp=deleted.facastamp) from deleted where faca.facastamp=deleted.facastampUPDATE FACC SET facc.totaladirec=facc.totaladirec-(select sum(deleted.valrec) from deleted where facc.facstamp=deleted.facstamp),facc.etotaladirec=facc.etotaladirec-(select sum(deleted.evalrec) from deleted where facc.facstamp=deleted.facstamp) from deleted where facc.facstamp=deleted.facstampUPDATE FAC SET fac.adiantado=fac.adiantado+(select sum(deleted.valrec) from deleted where fac.facstamp=deleted.facstamp),fac.eadiantado=fac.eadiantado+(select sum(deleted.evalrec) from deleted where fac.facstamp=deleted.facstamp) from deleted where fac.facstamp=deleted.facstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_udefs_delete ON UDEFS WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONif (select count(*) from gseln inner join deleted on gseln.udefsstamp=deleted.udefsstamp)>0 update gseln set gseln.udefsstamp='', gseln.descricao='' from deleted where gseln.udefsstamp=deleted.udefsstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cl_delete ON CL WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIf (select count(*) from ag inner join deleted on ag.clno=deleted.no and ag.clestab=deleted.estab )>0 update ag set ag.clno=0, ag.clestab=0,ag.ecl=0 from deleted where ag.clno=deleted.no and ag.clestab=deleted.estabIf (select count(*) from fl inner join deleted on fl.clno=deleted.no )>0 update fl set fl.clno=0, fl.ecl=0 from deleted where fl.clno=deleted.no GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mt_delete ON MT WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate va set va.kmsatehoje=isnull((select max(kms) from it (nolock) where it.matricula=va.matricula)-va.kmsinicial,0)update va set va.kmsatehoje=isnull((select max(kms) from mt (nolock) where mt.matricula=va.matricula and mt.realizada=1 and mt.kms>va.kmsatehoje+va.kmsinicial)-va.kmsinicial,va.kmsatehoje)update va set va.ultimokm=va.kmsatehoje+va.kmsinicial
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chl_delete ON chl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.chlstamp=deleted.chlstamp  IF (select COUNT(*) from chl, deleted where chl.chstamp = deleted.chstamp) > 0  BEGIN  UPDATE ch  SET ch.status =  (select top 1 cm  from ( select chl.cm, chl.ousrdata, chl.ousrhora     from chl INNER JOIN ch ON ch.chstamp = chl.chstamp    inner join deleted on deleted.chstamp = ch.chstamp    union all  select ox.cm, oy.ousrdata, oy.ousrhora  from ox inner join oy on ox.oxstamp = oy.oxstamp  inner join deleted on deleted.chstamp = oy.chstamp	  )t  order by ousrdata+ousrhora desc)   from deleted, ch where deleted.chstamp = ch.chstamp   END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mh_update ON MH WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (psobs) or update(tecnico) or update(zona) or update(nocopias) or update(data) or update(mastamp) BEGIN if (select count(*) from inserted where inserted.mastamp<>'')>0update ma set ma.psobs=inserted.psobs, ma.utecn=inserted.tecnico, ma.utecnnm=inserted.tecnnm,ma.udata=inserted.data,ma.ucopi=inserted.nocopias from inserted where ma.mastamp=inserted.mastamp and ma.udata<=inserted.data  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ttapa_update ON ttapa WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE tta SET tta.hreal=tta.hreal-deleted.hreal from DELETED where tta.ttastamp=deleted.ttastamp UPDATE TTA SET tta.hreal=tta.hreal+inserted.hreal from INSERTED where tta.ttastamp=inserted.ttastamp UPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tfa_update ON tfa WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(resumo) BEGIN UPDATE tta SET tta.tfad=inserted.resumo from INSERTED where tta.tfastamp=inserted.tfastamp END UPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tn_changed ON TN WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (changed) BEGIN update tn set tn.quem=inserted.quem, tn.dpt=inserted.dpt, tn.qualquer=inserted.qualquer,tn.pquem=inserted.pquem from inserted where tn.oristamp=inserted.oristamp END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_olbc_update ON olbc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE OL set ol.transf=1,ol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.dvalor=inserted.dvalor,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.data=inserted.data,ol.descricao=inserted.descricao,ol.said=inserted.valor,ol.esaid=inserted.evalor,ol.cheque=inserted.cheque,ol.documento=inserted.documento,ol.operext=inserted.operext,ol.cecope=inserted.cecopefrom inserted where inserted.contado>0 and ol.olstamp=inserted.olbcstampUPDATE OL set ol.transf=1,ol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.dvalor=inserted.dvalor,ol.data=inserted.data,ol.descricao=inserted.descricao,ol.entr=inserted.valor,ol.eentr=inserted.evalor,ol.cheque=inserted.cheque,ol.documento=inserted.documento,ol.operext=inserted.operext,ol.cecope=inserted.cecopefrom inserted where inserted.contado>0 and ol.olstamp=substring('C'+inserted.olbcstamp,1,25)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_gpc_insert ON gpc WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (olstamp,gpcstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,sgrupo,grupo,ollocal,olcodigo,moeda,intid )select inserted.gpcstamp,inserted.gpcstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'GPC',inserted.olcheque,inserted.documento,inserted.ccusto,inserted.ncusto,inserted.data,inserted.data,inserted.fref,inserted.descricao,inserted.total,inserted.etotal,inserted.contado,inserted.local,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,'PTE ou EURO',inserted.intid from inserted where inserted.contado>0 and inserted.olbancos=1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ngstatus_update ON NGSTATUS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(status) Begin update ng set ng.status=inserted.status from inserted inner join deleted on deleted.ngstatusstamp=inserted.ngstatusstamp where ng.status=deleted.status update vi set vi.ngstatus=inserted.status from inserted inner join deleted on deleted.ngstatusstamp=inserted.ngstatusstamp where vi.ngstatus=deleted.status update ft2 set ft2.ngstatus=inserted.status from inserted inner join deleted on deleted.ngstatusstamp=inserted.ngstatusstamp where ft2.ngstatus=deleted.status update bo2 set bo2.ngstatus=inserted.status from inserted inner join deleted on deleted.ngstatusstamp=inserted.ngstatusstamp where bo2.ngstatus=deleted.status update rd set rd.ngstatus=inserted.status from inserted inner join deleted on deleted.ngstatusstamp=inserted.ngstatusstamp where rd.ngstatus=deleted.status end 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chfl_delete ON CHFL WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE chf
SET 
chf.status = isnull((select top 1 chfl.cm from chfl where deleted.chfstamp = chfl.chfstamp order by chfl.lordem desc),1)
, chf.datastatus = isnull((select top 1 chfl.data from chfl where deleted.chfstamp = chfl.chfstamp order by chfl.lordem desc),getdate())
, chf.deststamp = CASE WHEN deleted.cm = 4 THEN '' ELSE chf.deststamp END 
from chf inner join deleted on chf.chfstamp = deleted.chfstamp				GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ng_delete ON ng WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON			delete from cptng where cptng.ngstamp=(select deleted.ngstamp from deleted)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_olbb_insert ON olbb WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (transf,olstamp,olbbstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,intid,ccusto,ncusto,dvalor,local,ollocal,contado,data,descricao,said,esaid,saidm,moeda,olcodigo,grupo,sgrupo,cheque,documento,operext,cecope )select 1,inserted.olbbstamp,inserted.olbbstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BB',inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.dvalor,'B',inserted.ollocal1,inserted.contado1,inserted.data,inserted.descricao,inserted.valor,inserted.evalor,inserted.valorm1,inserted.moeda1,'Trf','Transferências','Transferências',inserted.cheque,inserted.documento,inserted.operext,inserted.cecopefrom inserted where inserted.contado1>0 INSERT INTO OL (transf,olstamp,olbbstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,intid,ccusto,ncusto,dvalor,local,ollocal,contado,data,descricao,entr,eentr,entrm,moeda,olcodigo,grupo,sgrupo,cheque,documento,operext,cecope )select 1,substring('C'+inserted.olbbstamp,1,25),inserted.olbbstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BB',inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.dvalor,'B',inserted.ollocal2,inserted.contado2,inserted.data,inserted.descricao,inserted.valor,inserted.evalor,inserted.valorm2,inserted.moeda2,'Trf','Transferências','Transferências',inserted.cheque,inserted.documento,inserted.operext,inserted.cecopefrom inserted where inserted.contado2>0 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cm2_update ON cm2 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE (cmdesc)BEGINupdate cm1 set cm1.fosln = inserted.cmdesc from inserted  where cm1.fosl=inserted.cm update ts set ts.cmdesc = inserted.cmdesc from inserted  where ts.cmstocks=inserted.cm update ts set ts.cm2desc = inserted.cmdesc from inserted  where ts.cm2stocks=inserted.cm update td set td.cmsln = inserted.cmdesc from inserted  where td.cmsl=inserted.cm update sl set sl.cmdesc = inserted.cmdesc from inserted  where sl.cm=inserted.cm ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_owli_delete ON OWLI WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.owlistamp=deleted.owlistamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_Para1_insert ON PARA1 WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select COUNT(*) from inserted where descricao = 't_para1.dDataUltimaT_Para1' or descricao = 't_para1.cHoraUltimaT_Para1') = 0BEGINif (select COUNT(*) from para1 (nolock) where descricao = 't_para1.dDataUltimaT_Para1') = 0BEGINinsert into para1 (descricao, para1stamp) values ('t_para1.dDataUltimaT_Para1', substring(cast(newid() as char(36)),1,25))insert into para1 (descricao, para1stamp) values ('t_para1.cHoraUltimaT_Para1', substring(cast(newid() as char(36)),1,25))ENDupdate para1 set valor = convert(varchar, getdate(), 104) where descricao = 't_para1.dDataUltimaT_Para1'update para1 set valor = convert(varchar, getdate(), 108) where descricao = 't_para1.cHoraUltimaT_Para1'ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pc_delete ON PC WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM mca
from deleted
where deleted.conta=mca.conta and deleted.ano=mca.anoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ft_delete ON ft WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIF (select count(*) from deleted inner join td on td.ndoc=deleted.ndoc and td.lancacli=0 and td.regrd = 0
inner join cl on cl.no=deleted.no And cl.estab=deleted.estab
where (deleted.tipodoc=1 or deleted.tipodoc=3) )>0
UPDATE CL SET cl.acmfact=cl.acmfact-deleted.ttiliq, cl.eacmfact=cl.eacmfact-deleted.ettiliq from DELETED
 inner join td on td.ndoc=deleted.ndoc and td.lancacli=0 and td.regrd = 0
 where cl.no=deleted.no and deleted.anulado =0 and (deleted.tipodoc=1 or deleted.tipodoc=3)
DELETE from CH from deleted where ch.ftstamp=deleted.ftstamp
DELETE from CC from deleted where cc.ftstamp=deleted.ftstamp
DELETE from OL from deleted where ol.ftstamp=deleted.ftstamp
DELETE from ftcc from deleted where ftcc.ftstamp=deleted.ftstamp
if (select count(*) from deleted where deleted.lrstamp<>'')>0
UPDATE LR SET lr.edebi=lr.edebi-deleted.total,
lr.eedebi=lr.eedebi-deleted.etotal from deleted where lr.lrstamp=deleted.lrstamp
if (select count(*) from deleted where deleted.lpstamp<>'')>0
UPDATE LP SET lp.edebi=lp.edebi-deleted.total,
lp.eedebi=lp.eedebi-deleted.etotal from deleted where lp.lpstamp=deleted.lpstamp
delete from ftt from ftt inner join deleted on ftt.ftstamp =deleted.ftstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ttapa_insert ON ttapa WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TTA SET tta.hreal=tta.hreal+inserted.hreal from INSERTED where tta.ttastamp=inserted.ttastamp UPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cbbl_delete ON cbbl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON				UPDATE cc SET
					cbbno=0
				from deleted d
				inner join cc on cc.ccstamp=d.ccstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_su_delete ON SU WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIF (select count(*) from rq inner join deleted on rq.nosug=deleted.nosug)>0 update rq set rq.nosug=0,rq.sustamp='' from deleted where rq.nosug=deleted.nosug IF (select count(*) from rq inner join deleted on rq.nosugbuild=deleted.nosug)>0 update rq set rq.nosugbuild=0,rq.sustampbuild='',rq.desenvolve=0,rq.build='',rq.dias=0 from deleted where rq.nosugbuild=deleted.nosug 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_le_update ON le WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF update(plano) AND (select COUNT(*) from inserted where plano=0)>0
UPDATE li 
SET plano=0 
from inserted i
where li.lestamp=i.lestamp and i.plano=0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fmfr_update ON FMFR WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (nomeabrv) BEGIN if update(nomeabrv) BEGINupdate fm set fm.nomefmfr=inserted.nomeabrv from inserted where fm.numfmfr=inserted.numero END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cc_insert ON CC WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONdeclare @m_banchab char(60), @m_contado float
select @m_banchab = ISNULL((select valor from para1 where descricao='m_banchab'),'')
SELECT @m_contado = ISNULL((SELECT TOP 1 noconta FROM bl WHERE bl.banco = SUBSTRING(@m_banchab, 1, 10) AND bl.conta = SUBSTRING(@m_banchab, 12, 20)),0)
UPDATE CL SET
cl.saldo=cl.saldo+(select Sum(inserted.deb-inserted.cred+inserted.difcambio) from inserted where inserted.no=cl.no),
cl.esaldo=cl.esaldo+(select Sum(inserted.edeb-inserted.ecred+inserted.edifcambio) from inserted where inserted.no=cl.no)
, cl.pcktsyncdate = convert(datetime, substring(convert(char,getdate()),1,12)), cl.pcktsynctime = CONVERT(CHAR(8),GETDATE(),8)
from INSERTED where cl.no=inserted.no
if ( select count(*) from inserted where inserted.cr<>0 and inserted.docref<>0 and inserted.origem='CC')>0
UPDATE CC SET
cc.debf = cc.debf+inserted.cred, cc.credf = cc.credf+inserted.deb,
cc.edebf = cc.edebf+inserted.ecred, cc.ecredf = cc.ecredf+inserted.edeb
from INSERTED where cc.cm=inserted.cr and cc.nrdoc=inserted.docref and cc.no=inserted.no and inserted.origem='CC'
and cc.ccstamp = inserted.occstamp
INSERT INTO OD (ccstamp,odstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,origem,
documento,cheque,
data,entr,eentr,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,moeda,boacobranca)
select inserted.ccstamp,inserted.ccstamp,
inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'CC',
inserted.cmdesc,ltrim(str(inserted.nrdoc,10,0)),dateadd(day,cl.odatraso,inserted.dataven)
,(inserted.deb-inserted.debf)-(inserted.cred-inserted.credf) 												- (inserted.ivacativado - abs(inserted.ivacatreg))
,(inserted.edeb-inserted.edebf)-(inserted.ecred-inserted.ecredf) 													- (inserted.eivacativado - abs(inserted.eivacatreg))
,'Rec :'+inserted.nome,cm1.olcl,cm1.clgrupo,cm1.clsgrupo,
inserted.fref,inserted.ccusto,inserted.ncusto
 , case when isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0 then @m_banchab else isnull(ft2.vdollocal,@m_banchab) end as contam_banchab
 ,case when isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0  then @m_contado else ft2.vdcontado end as contacontado
  ,case when NOT(isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0) then ft2.vdlocal
 else case when @m_contado>0 then 'B' else 'C' end end as contalocal
 ,case when isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0 then 'PTE ou EURO' else ft2.olmoeda end as contaolmoeda
 ,case when isnull(cl.cobnao,0) = 0 then cm1.boacobranca else 0 end as boacob
 from inserted  inner join cl on cl.no=inserted.no and cl.estab = inserted.estab inner join cm1 on cm1.cm=inserted.cm  left join ft2 on ft2.ft2stamp = inserted.ftstamp  left join ft on ft.ftstamp = ft2.ft2stamp
 left join td on td.ndoc=ft.ndoc
 where ((inserted.edeb-inserted.edebf)-(inserted.ecred-inserted.ecredf)<>0 and (inserted.deb-inserted.debf)-(inserted.cred-inserted.credf)<>0)
if (select COUNT(*) from inserted where inserted.formapag='')>0
UPDATE cc SET formapag=1
from inserted
WHERE cc.ccstamp=inserted.ccstamp AND cc.formapag=''GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tb_update ON tb WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE from OL from deleted where ol.tbstamp = deleted.tbstamp and deleted.tbstamp not in (select inserted.tbstamp from inserted where inserted.contado>0 and inserted.olbancos=1)INSERT INTO OL (olstamp,tbstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,sgrupo,grupo,ollocal,olcodigo )select inserted.tbstamp,inserted.tbstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'TB',inserted.cheque,inserted.documento,inserted.ccusto,inserted.ncusto,inserted.data,inserted.data,inserted.fref,inserted.descricao,inserted.valor,inserted.evalor,inserted.contado,inserted.local,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigofrom inserted where inserted.contado>0 and inserted.olbancos=1 and inserted.tbstamp not IN (select deleted.tbstamp from deleted where deleted.contado>0 and deleted.olbancos=1)UPDATE OL SETol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.ousrinis=inserted.ousrinis,ol.ousrhora=inserted.ousrhora,ol.ousrdata=inserted.ousrdata,ol.origem='TB',ol.cheque=inserted.cheque,ol.documento=inserted.documento,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.data=inserted.data,ol.dvalor=inserted.data,ol.fref=inserted.fref,ol.descricao=inserted.descricao,ol.said=inserted.valor,ol.esaid=inserted.evalor,ol.contado=inserted.contado,ol.local=inserted.local,ol.sgrupo=inserted.sgrupo,ol.grupo=inserted.grupo,ol.ollocal=inserted.ollocal,ol.olcodigo=inserted.olcodigofrom inserted where inserted.contado>0 and inserted.olbancos=1 and inserted.tbstamp=ol.tbstamp and inserted.tbstamp IN (select deleted.tbstamp from deleted where deleted.contado>0 and deleted.olbancos=1)IF UPDATE (cheque) UPDATE PR SET pr.usrinis=inserted.usrinis,pr.usrhora=inserted.usrhora,pr.usrdata=inserted.usrdata,pr.ousrinis=inserted.ousrinis,pr.ousrhora=inserted.ousrhora,pr.ousrdata=inserted.ousrdata,pr.tbcheque=inserted.chequefrom inserted where inserted.tbstamp=pr.tbstampIF UPDATE (cheque) UPDATE FR SET fr.usrinis=inserted.usrinis,fr.usrhora=inserted.usrhora,fr.usrdata=inserted.usrdata,fr.ousrinis=inserted.ousrinis,fr.ousrhora=inserted.ousrhora,fr.ousrdata=inserted.ousrdata,fr.tbcheque=inserted.chequefrom inserted where inserted.tbstamp=fr.tbstampIF UPDATE (cheque) UPDATE VR SET vr.usrinis=inserted.usrinis,vr.usrhora=inserted.usrhora,vr.usrdata=inserted.usrdata,vr.ousrinis=inserted.ousrinis,vr.ousrhora=inserted.ousrhora,vr.ousrdata=inserted.ousrdata,vr.tbcheque=inserted.chequefrom inserted where inserted.tbstamp=vr.tbstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cc_delete ON CC WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE CL SET cl.saldo=cl.saldo - (select sum(deleted.deb-deleted.cred+deleted.difcambio) from deleted where deleted.no=cl.no), cl.esaldo=cl.esaldo - (select sum(deleted.edeb-deleted.ecred+deleted.edifcambio) from deleted where deleted.no=cl.no) , cl.pcktsyncdate = convert(datetime, substring(convert(char,getdate()),1,12)), cl.pcktsynctime = CONVERT(CHAR(8),GETDATE(),8) from deleted where cl.no=deleted.noif (select count(*) from deleted where RTRIM(LTRIM(deleted.occstamp)) = '') > 0  UPDATE CC SET cc.debf = cc.debf-deleted.cred, cc.credf = cc.credf-deleted.deb, cc.edebf = cc.edebf-deleted.ecred, cc.ecredf = cc.ecredf-deleted.edeb from deleted where cc.cm=deleted.cr and cc.nrdoc=deleted.docref and cc.no=deleted.noif (select count(*) from deleted where RTRIM(LTRIM(deleted.occstamp)) <> '') > 0  UPDATE CC SET cc.debf = cc.debf-deleted.cred, cc.credf = cc.credf-deleted.deb, cc.edebf = cc.edebf-deleted.ecred, cc.ecredf = cc.ecredf-deleted.edeb from deleted where cc.cm=deleted.cr and cc.nrdoc=deleted.docref and cc.no=deleted.no and cc.ccstamp = deleted.occstampDELETE from OD from deletedwhere od.ccstamp=deleted.ccstamp  UPDATE PN SET pn.pago=0 from deleted where deleted.ftstamp>space(25) and ((deleted.deb-deleted.debf)-(deleted.cred-deleted.credf)=0 or (deleted.edeb-deleted.edebf)-(deleted.ecred-deleted.ecredf)=0) and pn.ftstamp=deleted.ftstamp and (select td.ccautoreg from td inner join ft on td.ndoc=ft.ndoc where ft.ftstamp=pn.ftstamp)<>1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lg_update ON lg WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF update(plano) AND (select COUNT(*) from inserted where plano=0)>0
UPDATE ll 
SET plano=0 
from inserted i
where ll.lgstamp=i.lgstamp and i.plano=0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_st_update ON ST WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF (update(ref) or update(design)) BEGINIF (select count(*) from bc inner join inserted on bc.ststamp=inserted.ststamp )>0 update bc set bc.ref=inserted.ref,bc.design=inserted.design from bc inner join inserted on bc.ststamp=inserted.ststampENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fc_delete ON FC WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate FL
set fl.saldo = case when
fl.saldo - isnull((select Sum((deleted.cred-deleted.deb+deleted.difcambio)
+ case when (deleted.cred-deleted.deb+deleted.difcambio) > 0 then -deleted.ivacativado
else deleted.ivacativado end)
from deleted where deleted.no=fl.no and deleted.cm <> 0),0) <> 0
then
fl.saldo - isnull((select Sum((deleted.cred-deleted.deb+deleted.difcambio)
+ case when (deleted.cred-deleted.deb+deleted.difcambio) > 0 then -deleted.ivacativado
else deleted.ivacativado end)
from deleted where deleted.no=fl.no and deleted.cm <> 0),0)
else
0
end,
fl.esaldo = case when
fl.esaldo - isnull((select Sum((deleted.ecred-deleted.edeb+deleted.edifcambio)
+ case when (deleted.ecred-deleted.edeb+deleted.edifcambio) > 0 then -deleted.eivacativado
else deleted.eivacativado end)
from deleted where deleted.no=fl.no and deleted.cm <> 0),0) <> 0
then
fl.esaldo - isnull((select Sum((deleted.ecred-deleted.edeb+deleted.edifcambio)
+ case when (deleted.ecred-deleted.edeb+deleted.edifcambio) > 0 then -deleted.eivacativado
else deleted.eivacativado end)
from deleted where deleted.no=fl.no and deleted.cm <> 0),0)
else
0
end
from deleted
where fl.no =deleted.no

update FC
set fc.credf = fc.credf-deleted.deb, fc.debf = fc.debf-deleted.cred
, fc.ecredf = fc.ecredf-deleted.edeb, fc.edebf = fc.edebf-deleted.ecred
, fc.credfm = fc.credfm-deleted.debm, fc.debfm = fc.debfm-deleted.credm
from deleted
where (fc.cm=deleted.cr and fc.adoc=deleted.cradoc and fc.no=deleted.no and deleted.cr > 0 and deleted.cradoc <> space(10))
or (fc.fcstamp = substring(deleted.fcstamp,2,25) and substring(deleted.fcstamp,1,1)= 'R')

delete from od from deleted where od.fcstamp=deleted.fcstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftma_delete ON ftma WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.nome=case when deleted.tipodoc=3 then ft.nome else '' end,ma.no=case when deleted.tipodoc=3 then ft.no else 0 end,ma.ftnmdoc=case when deleted.tipodoc=3 then ma.ftnmdoc else '' end,ma.ftfno=case when deleted.tipodoc=3 then ma.ftfno else 0 end,ma.ftfdata=case when deleted.tipodoc=3 then ma.ftfdata else '' end,ma.ftstamp=case when deleted.tipodoc=3 then ma.ftstamp else '' end,ma.casa=case when deleted.tipodoc=3 then 0 else 1 end,ma.morada=case when deleted.tipodoc=3 then ft.morada else '' end,ma.local=case when deleted.tipodoc=3 then ft.local else '' end,ma.codpost=case when deleted.tipodoc=3 then ft.codpost else '' end,ma.zona=case when deleted.tipodoc=3 then ft.zona else '' end,ma.vendnm=case when deleted.tipodoc=3 then ft.vendnm else '' end,ma.vendedor=case when deleted.tipodoc=3 then ft.vendedor else 0 end,ma.instal=case when deleted.tipodoc=3 then ma.instal else '' end,ma.telefone=case when deleted.tipodoc=3 then cl.telefone else '' end,ma.contacto=case when deleted.tipodoc=3 then cl.contacto else '' end,ma.estab=case when deleted.tipodoc=3 then ft.estab else 0 end,ma.email=case when deleted.tipodoc=3 then cl.email else '' end,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.fdata,121))+4,datalength(ma.obs)), ma.ftorigem=case when deleted.tipodoc=3 then ma.ftorigem else ''  end from deleted inner join ma on deleted.mastamp=ma.mastamp inner join ft on ma.ftstamp=ft.ftstamp inner join cl on cl.no=ft.no and cl.estab=ft.estab IF (select COUNT(*) from deleted where deleted.serprvef = 1) > 0 UPDATE MA SET ma.serie = deleted.maserierpl from deleted where deleted.serprvef = 1 and deleted.mastamp=ma.mastamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tno_update ON tno WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ll_insert ON ll WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE FC SET fc.credf=fc.credf+case when inserted.rec-inserted.arred>0 then inserted.rec-inserted.arred else 0 end from inserted where inserted.fcstamp=fc.fcstampupdate fc set fc.debf=fc.debf-case when inserted.rec-inserted.arred<0 then inserted.rec-inserted.arred else 0 end from inserted where inserted.fcstamp=fc.fcstampupdate fc set fc.ecredf=fc.ecredf+case when inserted.erec>0 then inserted.erec-inserted.earred else 0 end from inserted where inserted.fcstamp=fc.fcstampupdate fc set fc.edebf=fc.edebf-case when inserted.erec<0 then inserted.erec-inserted.earred else 0 end from inserted where inserted.fcstamp=fc.fcstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_po_delete ON po WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete from fc from deleted where fc.postamp =deleted.postamp
delete from OL from deleted where ol.postamp =deleted.postampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ff_update ON ff WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON			if update(data)
				update ff set
					ff.diai = case when month(i.data)<10 then '0' else '' end + CAST(month(i.data) as varchar(2)) + case when day(i.data)<10 then '0' else '' end + CAST(day(i.data) as varchar(2))
				from inserted i where ff.ffstamp=i.ffstamp

			if update(dataf)
				update ff set
					ff.diaf = case when month(i.dataf)<10 then '0' else '' end + CAST(month(i.dataf) as varchar(2)) + case when day(i.dataf)<10 then '0' else '' end + CAST(day(i.dataf) as varchar(2))
				from inserted i where ff.ffstamp=i.ffstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lm_delete ON lm WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from OD from deleted where od.lmstamp=deleted.lmstampDELETE from CC from deleted where cc.lmstamp=deleted.lmstampDELETE from OL from deleted where ol.lmstamp=deleted.lmstamp DELETE from CH from deleted where ch.lmstamp=deleted.lmstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pochf_insert ON POCHF WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE chf
SET deststamp = inserted.pochfstamp
, destino = 'POCHF'
, nome = case when chf.nome = '' then po.nome else chf.nome end 
, no = case when chf.no = 0 then po.no else chf.no end 
, estab = case when chf.no = 0 then po.estab else chf.estab end 
from inserted inner join chf on inserted.chfstamp = chf.chfstamp 
inner join po on po.postamp = inserted.postamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_gpc_delete ON gpc WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.gpcstamp=deleted.gpcstampUPDATE PR SETpr.gpccheque='',pr.gpcstamp='',pr.gpcok=0from deleted where pr.gpcstamp=deleted.gpcstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vr_insert ON vr WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (olstamp,vrstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,intid,sgrupo,grupo,ollocal,olcodigo,moeda,saidm )select inserted.vrstamp,inserted.vrstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'VR',inserted.cheque,(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),inserted.ccusto,inserted.ncusto,inserted.oldata,inserted.data,inserted.fref,'Pagamento a '+inserted.nome,inserted.vfin,inserted.evfin,inserted.contado,inserted.telocal,inserted.intid,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,inserted.moeda,inserted.mvfin from inserted where inserted.lol=1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_gpc_update ON gpc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE from OL from deleted where ol.gpcstamp = deleted.gpcstamp and deleted.gpcstamp not in (select inserted.gpcstamp from inserted where inserted.contado>0 and inserted.olbancos=1)INSERT INTO OL (olstamp,gpcstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,sgrupo,grupo,ollocal,olcodigo,moeda,intid )select inserted.gpcstamp,inserted.gpcstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'GPC',inserted.olcheque,inserted.documento,inserted.ccusto,inserted.ncusto,inserted.data,inserted.data,inserted.fref,inserted.descricao,inserted.total,inserted.etotal,inserted.contado,inserted.local,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,'PTE ou EURO',inserted.intid from inserted where inserted.contado>0 and inserted.olbancos=1 and inserted.gpcstamp not IN (select deleted.gpcstamp from deleted where deleted.contado>0 and deleted.olbancos=1)UPDATE OL SETol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.ousrinis=inserted.ousrinis,ol.ousrhora=inserted.ousrhora,ol.ousrdata=inserted.ousrdata,ol.origem='GPC',ol.cheque=inserted.olcheque,ol.documento=inserted.documento,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.data=inserted.data,ol.dvalor=inserted.data,ol.fref=inserted.fref,ol.descricao=inserted.descricao,ol.said=inserted.total,ol.esaid=inserted.etotal,ol.contado=inserted.contado,ol.intid=inserted.intid,ol.local=inserted.local,ol.sgrupo=inserted.sgrupo,ol.grupo=inserted.grupo,ol.ollocal=inserted.ollocal,ol.olcodigo=inserted.olcodigo,ol.moeda='PTE ou EURO' from inserted where inserted.contado>0 and inserted.olbancos=1 and inserted.gpcstamp=ol.gpcstamp and inserted.gpcstamp IN (select deleted.gpcstamp from deleted where deleted.contado>0 and deleted.olbancos=1)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vr_delete ON vr WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.vrstamp=deleted.vrstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vs_update ON VS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE(area)BEGINUPDATE vr SET vr.area = inserted.area FROM inserted INNER JOIN vr ON inserted.no = vr.noEND
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fi2_delete ON fi2 WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE BI SET
bi.fno = 0
, bi.adoc = ''
, bi.nmdoc = ''
, bi.oftstamp = ''
, bi.ndoc = 0
, bi.qtt2 = bi.qtt2-isnull(
(select SUM(fi.qtt *(case when fi.tipodoc=3 then -1 else 1 end))
from fi
inner join deleted on deleted.fi2stamp=fi.fistamp
where fi.bistamp <> '' and fi.bistamp = bi.bistamp and fi.fistamp = deleted.fi2stamp and deleted.noupdqtt2ori=0) ,0)
from bi
inner join fi on fi.bistamp = bi.bistamp
inner join ft ON fi.ftstamp = ft.ftstamp
inner join deleted on fi.fistamp = deleted.fi2stamp
where fi.bistamp<>'' AND ft.anulado=0
IF exists (select 1 from deleted where deleted.oristamp <> '' AND deleted.origem='CHL') 
UPDATE CHL 
SET chl.FTSTAMP = ''
from chl 
inner join deleted on chl.chlstamp=deleted.oristamp and deleted.origem='CHL'GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ch_insert ON CH WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON					-- &&movimento aceite feito via insert directo
				    if (select count(*) from inserted where inserted.origem<> 'CH' AND inserted.status = 0) > 0
				    BEGIN
						update ch 
						set  ch.status = 1
						from ch, inserted
						where ch.chstamp = inserted.chstamp
						
						Insert into chl (chlstamp, data, cm, chldesc, valor, evalor, moeda, valorm, ollocal, contado, chstamp, origem, ousrinis, ousrdata
											, ousrhora, usrinis, usrdata, usrhora) 
										(select  inserted.chstamp, inserted.dataem, 1, 'Aceite', inserted.valor, inserted.evalor, inserted.moeda
											, inserted.valorm,  inserted.ollocal, inserted.contado
											, inserted.chstamp, inserted.origem, inserted.ousrinis, inserted.ousrdata
											, inserted.ousrhora, inserted.usrinis, inserted.usrdata, inserted.usrhora from inserted)		 			
				    END
					
			
				    if (select count(*) from inserted where inserted.choristamp <> '') > 0
				    BEGIN
						update ch 
						set ch.chdesstamp = inserted.chstamp
							, ch.status = 4
						from ch, inserted
						where ch.chstamp = inserted.choristamp
						
						Insert into chl (chlstamp, data, cm, chldesc, valor, evalor, moeda, valorm, obs, ollocal, contado, chstamp, origem, ousrinis, ousrdata
											, ousrhora, usrinis, usrdata, usrhora) 
										(select inserted.chstamp, inserted.dataem, 4, 'Substituição do Título de tesouraria', inserted.valor, inserted.evalor, inserted.moeda
											, inserted.valorm, 'Substituído por:'+ inserted.clcheque, inserted.ollocal, inserted.contado
											, inserted.choristamp, 'CH', inserted.ousrinis, inserted.ousrdata
											, inserted.ousrhora, inserted.usrinis, inserted.usrdata, inserted.usrhora from inserted)		 			
				    END 
				    
				    
				   			    GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ch_delete ON CH WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON				 if (select count(*) from deleted where deleted.choristamp <> '') > 0
					 BEGIN 
					 	DELETE FROM CHL FROM deleted, chl WHERE deleted.chstamp = chl.chlstamp AND chl.cm = 4
					 				
						 UPDATE ch  SET ch.status = 
						 (select top 1 cm  from (
						 select chl.cm, chl.ousrdata, chl.ousrhora    
						 from chl INNER JOIN ch ON ch.chstamp = chl.chstamp   
						 inner join deleted on deleted.choristamp = ch.chstamp   
						 union all 
						 select ox.cm, oy.ousrdata, oy.ousrhora 
						 from ox inner join oy on ox.oxstamp = oy.oxstamp 
						 inner join deleted on deleted.choristamp  = oy.chstamp	 
						 )t 
						 order by ousrdata+ousrhora desc)  
						 from deleted, ch where deleted.choristamp = ch.chstamp  
						
					 END 
				 ELSE
				 	BEGIN 
				 	 	DELETE FROM CHL FROM deleted, chl WHERE deleted.chstamp = chl.chlstamp
				 	END	 
				 	
				 	
				 	GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_it_insert ON IT WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONupdate va set va.custototal=va.custototal+inserted.tt, va.ecustotota=va.ecustotota+inserted.ett from inserted where va.matricula=inserted.matriculaupdate va set va.kmsatehoje=inserted.kms-va.kmsinicial, va.ultimokm=inserted.kms, va.ultimadata=inserted.data from inserted where inserted.matricula=va.matricula and inserted.kms>va.ultimokmupdate vc set vc.matricula=inserted.matricula, vc.custototal=vc.custototal+inserted.tt, vc.ecustotota=vc.ecustotota+inserted.ett, vc.ultimadata=case when vc.ultimadata<inserted.data then inserted.data else vc.ultimadata end from inserted where vc.idacess=inserted.idacess
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_caltc_update ON caltc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(ldata) and (select count(*) from inserted where inserted.alteracal = 1) > 0 UPDATE cal SET cal.ldata=cal.pdata + inserted.ldatafrom inserted where inserted.alteracal=1 and inserted.caltcstamp=cal.tipostamp and cal.fechado = 0 if update(usr1) and (select count(*) from inserted where inserted.alteracal = 1) > 0 UPDATE cal SET cal.usr1=inserted.usr1 from inserted where inserted.alteracal=1 and inserted.caltcstamp=cal.tipostamp and cal.fechado = 0 if update(usr2) and (select count(*) from inserted where inserted.alteracal = 1) > 0 UPDATE cal SET cal.usr2=inserted.usr2 from inserted where inserted.alteracal=1 and inserted.caltcstamp=cal.tipostamp and cal.fechado = 0 if update(usr3) and (select count(*) from inserted where inserted.alteracal = 1) > 0 UPDATE cal SET cal.usr3=inserted.usr3 from inserted where inserted.alteracal=1 and inserted.caltcstamp=cal.tipostamp and cal.fechado = 0 if update(usr4) and (select count(*) from inserted where inserted.alteracal = 1) > 0 UPDATE cal SET cal.usr4=inserted.usr4 from inserted where inserted.alteracal=1 and inserted.caltcstamp=cal.tipostamp and cal.fechado = 0 if update(usr5) and (select count(*) from inserted where inserted.alteracal = 1) > 0 UPDATE cal SET cal.usr5=inserted.usr5 from inserted where inserted.alteracal=1 and inserted.caltcstamp=cal.tipostamp and cal.fechado = 0 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tex_update ON tex WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(texd) BEGIN UPDATE texta SET texta.texd=inserted.texd from INSERTED where texta.texstamp=inserted.texstamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fl_delete ON FL WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIF (select count(*) from ag inner join deleted on ag.flno=deleted.no and ag.flestab=deleted.estab )>0 update ag set ag.flno=0, ag.flestab=0,ag.efl=0 from deleted where ag.flno=deleted.no and ag.flestab=deleted.estab IF (select count(*) from cl inner join deleted on cl.flno=deleted.no )>0 update cl set cl.flno=0, cl.efl=0 from deleted where cl.flno=deleted.no GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_wtw_update ON wtw WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(wtwd) BEGIN UPDATE wtwa SET wtwa.wtwd=inserted.wtwd from INSERTED where wtwa.wtwstamp=inserted.wtwstamp UPDATE wwf SET wwf.wtwd=inserted.wtwd from INSERTED where wwf.wtwstamp=inserted.wtwstamp UPDATE wwfa SET wwfa.wtwd=inserted.wtwd from INSERTED where wwfa.wtwstamp=inserted.wtwstamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chf_insert ON CHF WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF exists(select 1 from inserted where inserted.origem = 'CHF' AND inserted.oristamp<>'')
begin 
UPDATE chf
SET chf.status = 4
, chf.datastatus = inserted.usrdata
, chf.deststamp = inserted.chfstamp 
, chf.destino = 'CHF'	
FROM chf inner join inserted on	chf.chfstamp = inserted.oristamp  			
end			

if exists(select 1 from inserted where inserted.status = 0) 
BEGIN
update chf   
set  chf.status = 1
, chf.datastatus = inserted.ousrdata
from chf, inserted
where chf.chfstamp = inserted.chfstamp 			
END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fn_delete ON fn WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM SL FROM deleted where sl.fnstamp=deleted.fnstamp and deleted.fnstamp<>''DELETE FROM SV FROM deleted where sv.fnstamp=deleted.fnstamp and deleted.fnstamp<>''If (select top 1 cm1.fonactqtt2 from deleted inner join fo on fo.fostamp=deleted.fostamp inner join cm1 on fo.doccode=cm1.cm)=0BeginUPDATE BI SET bi.adoc = '',bi.nmdoc = '',bi.ofostamp = '',bi.ndoc = 0, bi.qtt2 = bi.qtt2-(select SUM(deleted.qtt) from deleted where deleted.bistamp <> '' and deleted.bistamp = bi.bistamp) from bi inner join deleted on deleted.bistamp=bi.bistamp where deleted.bistamp<>'' EndIF (select count(fnstamp) from deleted where deleted.rpflstamp<>space(25) )>0 UPDATE FN SET fn.fnrpfl = space(25) from deleted where deleted.fnstamp=fn.fnrpfl 		
			if (select count(*) from inserted where fnofo<>0 and ndocfo<>0) >0 
			Begin
			UPDATE FO2 SET 
			fo2.facturada = 0,
			fo2.nmdocfo = '', 
			fo2.fnofo = 0,
			fo2.adocfo = ''
			from inserted
				inner join fo on 
					inserted.ndocfo = fo.doccode and inserted.fnofo = fo.no and inserted.foanofo = fo.foano  and inserted.adocfo = fo.adoc
				inner join fo2 on fo.fostamp = fo2.fo2stamp
				inner join cm1 on cm1.cm=fo.doccode  
			where inserted.ndocfo<>0 
			EndGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_caltl_insert ON caltl WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from caltc inner join inserted on inserted.caltcstamp = caltc.caltcstamp where caltc.alteracal = 1) > 0 INSERT INTO calp (username,userno,dpt,tipo,resumo,descricao,data,ldata,hora,caltlstamp,caltcstamp,calpstamp,calstamp,calno,emnome,emno,clnome,clno,clestab) select inserted.username,inserted.userno,inserted.dpt,inserted.tipo,inserted.resumo,inserted.descricao,getdate(),getdate() + inserted.ldata,cast(datepart(hh, getdate()) as char(2)) + ':' + cast(datepart(mi, getdate()) as char(2)),inserted.caltlstamp,caltc.caltcstamp,substring(cast(newid() as char(36)),1,25),cal.calstamp,cal.calno,cal.emnome,cal.emno,cal.clnome,cal.clno,cal.clestab from inserted inner join caltc on caltc.caltcstamp = inserted.caltcstamp inner join cal on cal.tipostamp = caltc.caltcstamp and cal.fechado = 0 where caltc.alteracal = 1 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mgdisk_delete ON mgdisk WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON	if exists (select * from deleted where deleted.mapaprinc = 1)
		delete from mod 
		from deleted
		where mod.oristamp = deleted.idustampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ng_update ON ng WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON			if update(codigo)
				update cptng set
					cptng.ngresumo=i.codigo
				from inserted as i
				where cptng.ngstamp=i.ngstamp
			if update(area)
				update cptng set
					cptng.area=i.area
				from inserted as i
				where cptng.ngstamp=i.ngstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mh_insert ON MH WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON IF update (psobs) or update(tecnico) or update(zona) or update(nocopias) or update(data)  BEGIN if (select count(*) from inserted where inserted.mastamp<>'')>0update ma set ma.psobs=inserted.psobs, ma.utecn=inserted.tecnico, ma.utecnnm=inserted.tecnnm,ma.udata=inserted.data,ma.ucopi=inserted.nocopias from inserted where ma.mastamp=inserted.mastamp and ma.udata<=inserted.data  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fo2_delete ON FO2 WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE chf
SET deststamp = ''
, destino = ''
from deleted inner join chf on deleted.chfstamp = chf.chfstamp 
where deleted.cheque <> ''					
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mhct_delete ON mhct WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdeclare @mserie varchar(25) set @mserie = (select distinct mh.serie from mh inner join deleted on mh.mhstamp = deleted.mhstamp) IF (select count(*) from deleted where deleted.valor>=0)>0 BEGIN UPDATE MACT SET mact.usrinis=deleted.usrinis,mact.usrhora=deleted.usrhora,mact.usrdata=deleted.usrdata,mact.valor = case when mact.valor<>deleted.valor then mact.valor else (SELECT ISNULL(Max(mhct.valor),0) from mhct (nolock) inner join mh (nolock) on mhct.mhstamp = mh.mhstamp where mhct.codcount = deleted.codcount and mh.serie = @mserie) end,mact.data = case when mact.data<>deleted.data then mact.data else (SELECT ISNULL(Max(mhct.data),cast('19000101' as datetime)) from mhct (nolock) inner join mh (nolock) on mhct.mhstamp = mh.mhstamp where mh.serie = @mserie) end from deleted inner join mact on mact.mactstamp=deleted.mactstamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mhct_update ON mhct WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONdeclare @mserie varchar(25) set @mserie = (select distinct mh.serie from mh inner join deleted on mh.mhstamp = deleted.mhstamp) IF (select count(*) from deleted where deleted.valor>=0)>0 BEGIN UPDATE MACT SET mact.usrinis=deleted.usrinis,mact.usrhora=deleted.usrhora,mact.usrdata=deleted.usrdata,mact.valor = case when mact.valor<>deleted.valor then mact.valor else (SELECT ISNULL(Max(mhct.valor),0) from mhct(nolock) inner join mh(nolock) on mhct.mhstamp = mh.mhstamp where mhct.codcount = deleted.codcount and mh.serie = @mserie) end,mact.data = case when mact.data<>deleted.data then mact.data else (SELECT ISNULL(Max(mhct.data),cast('19000101' as datetime)) from mhct (nolock) inner join mh (nolock) on mhct.mhstamp = mh.mhstamp where mh.serie = @mserie) end from deleted inner join mact on mact.mactstamp=deleted.mactstamp END if (select count(*) from inserted where inserted.valor>0)>0 BEGIN UPDATE MACT SET mact.usrinis=inserted.usrinis,mact.usrhora=inserted.usrhora,mact.usrdata=inserted.usrdata,mact.valor=case when mact.valor<inserted.valor then inserted.valor else mact.valor end, mact.data=case when mact.data<inserted.data then inserted.data else mact.data end from inserted inner join mact on mact.mactstamp=inserted.mactstamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cl_update ON CL WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIf (select count(*) from inserted where inserted.clivd=0)>0 BEGINIf (select count(*) from inserted inner join deleted on inserted.clstamp=deleted.clstamp where inserted.nome<>deleted.nome )>0 BEGINIf (select count(*) from cc inner join inserted on cc.no=inserted.no and inserted.clivd=0 )>0 update cc set cc.nome=inserted.nome from inserted where cc.no=inserted.no and cc.estab=inserted.estab and inserted.clivd=0 If (select count(*) from ft inner join inserted on ft.no=inserted.no and ft.estab=inserted.estab and inserted.clivd=0 )>0 update ft set ft.nome=inserted.nome from inserted,ft2 where ft.ftstamp=ft2.ft2stamp and ft.no=inserted.no and ft.estab=inserted.estab and inserted.clivd=0 and ft2.assinatura='' If (select count(*) from ft2 inner join inserted on ft2.c2no=inserted.no and ft2.c2estab=inserted.estab and inserted.clivd=0 )>0 update ft2 set ft2.c2nome=inserted.nome from inserted where ft2.c2no=inserted.no and ft2.c2estab=inserted.estab and inserted.clivd=0 and ft2.assinatura='' If (select count(*) from re inner join inserted on re.no=inserted.no and inserted.clivd=0 and (re.process=0 or re.rdata<'20131001') )>0 update re set re.nome=inserted.nome from inserted where re.no=inserted.no and re.estab=inserted.estab and inserted.clivd=0 and (re.process=0 or re.rdata<'20131001')  ENDIf (select count(*) from inserted inner join deleted on inserted.clstamp=deleted.clstamp where inserted.vencimento<>deleted.vencimento and inserted.estab=0 )>0 BEGINIf (select count(*) from cl inner join inserted on cl.no=inserted.no and cl.estab<>0 and inserted.estab=0 )>0 update cl set cl.vencimento=inserted.vencimento from inserted where cl.no=inserted.no and cl.estab<>0 and inserted.estab=0 ENDIf (select count(*) from inserted inner join deleted on inserted.clstamp=deleted.clstamp where inserted.alimite<>deleted.alimite and inserted.estab=0 )>0 BEGINIf (select count(*) from cl inner join inserted on cl.no=inserted.no and cl.estab<>0 and inserted.estab=0 )>0 update cl set cl.alimite=inserted.alimite from inserted where cl.no=inserted.no and cl.estab<>0 and inserted.estab=0 ENDIf (select count(*) from inserted inner join deleted on inserted.clstamp=deleted.clstamp where inserted.odatraso<>deleted.odatraso and inserted.estab=0 )>0 BEGINIf (select count(*) from cl inner join inserted on cl.no=inserted.no and cl.estab<>0 and inserted.estab=0 )>0 update cl set cl.odatraso=inserted.odatraso from inserted where cl.no=inserted.no and cl.estab<>0 and inserted.estab=0 ENDENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_td_update ON TD WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nmdoc) BEGIN  update WKDOCS set wkdocs.wkldocnome = inserted.nmdoc  from inserted, wkdocs, deleted   where wkdocs.wkldocnome=deleted.nmdoc and wkdocs.wkldocno=deleted.ndoc  update WK set wk.wkdocdescr = inserted.nmdoc   from inserted, wk, deleted  where wk.wkdocdescr=deleted.nmdoc  and wk.wkdoc= deleted.ndoc END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fofc_delete ON fofc WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from FC from deleted where fc.fofcstamp=deleted.fofcstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ohb_update ON OHB WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome) and (select count(*) from cv2 inner join deleted on cv2.nome=deleted.nome)>0 begin update cv2 set cv2.nome=inserted.nome from inserted inner join deleted on inserted.ohbstamp=deleted.ohbstamp where cv2.nome=deleted.nome end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rp_delete ON rp WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIF (SELECT COUNT(*) FROM deleted WHERE deleted.tipo = 3) = 0BEGINDELETE FROM ml FROM deleted WHERE ml.dostamp IN (SELECT ap.dostamp FROM ap WHERE ap.rpstamp = deleted.rpstamp)DELETE FROM do FROM deleted WHERE do.dostamp IN (SELECT ap.dostamp FROM ap WHERE ap.rpstamp = deleted.rpstamp)ENDELSEDELETE FROM mc FROM deleted WHERE mc.mcstamp IN (SELECT ap.dostamp FROM ap WHERE ap.rpstamp = deleted.rpstamp)DELETE FROM ap FROM deleted WHERE ap.rpstamp = deleted.rpstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tpo_update ON tpo WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(tpod) BEGIN UPDATE tta SET tta.tpod=inserted.tpod from INSERTED where tta.tpostamp=inserted.tpostamp UPDATE ttapa SET ttapa.tpod=inserted.tpod from INSERTED where ttapa.tpostamp=inserted.tpostamp UPDATE tli SET tli.tpod=inserted.tpod from INSERTED where tli.tpostamp=inserted.tpostamp UPDATE tfo SET tfo.tpod=inserted.tpod from INSERTED where tfo.tpostamp=inserted.tpostamp UPDATE tno SET tno.tpod=inserted.tpod from INSERTED where tno.tpostamp=inserted.tpostamp UPDATE tit SET tit.tpod=inserted.tpod from INSERTED where tit.tpostamp=inserted.tpostamp UPDATE tda SET tda.tpod=inserted.tpod from INSERTED where tda.tpostamp=inserted.tpostamp UPDATE tbu SET tbu.tpod=inserted.tpod from INSERTED where tbu.tpostamp=inserted.tpostamp UPDATE tmr SET tmr.tpod=inserted.tpod from INSERTED where tmr.tpostamp=inserted.tpostamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fopd_insert ON fopd WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE FC SET fc.difcambio=fc.difcambio+(SELECT SUM(inserted.difcambio) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.edifcambio=fc.edifcambio+(SELECT SUM(inserted.edifcambio) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.debf=fc.debf+(SELECT SUM(inserted.vreg - inserted.virs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.edebf=fc.edebf+(SELECT SUM(inserted.evreg - inserted.evirs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.debfm=fc.debfm+(SELECT SUM(inserted.vmreg) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.virsreg = fc.virsreg + (SELECT SUM(inserted.virs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.evirsreg = fc.evirsreg + (SELECT SUM(inserted.evirs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp) from fc inner join inserted on inserted.fcstamp = fc.fcstamp where inserted.fcstamp <> '' update FC set fc.credf=fc.credf+(SELECT SUM(inserted.vreg) from inserted where  inserted.fostamp <> '' and inserted.fostamp = fc.fostamp), fc.ecredf=fc.ecredf+(SELECT SUM(inserted.evreg) from inserted where  inserted.fostamp <> '' and inserted.fostamp = fc.fostamp), fc.credfm=fc.credfm+(SELECT SUM(inserted.vmreg) from inserted where  inserted.fostamp <> '' and inserted.fostamp = fc.fostamp) from fc inner join inserted on inserted.fostamp = fc.fostamp inner join pd on inserted.pdstamp = pd.pdstamp where inserted.fostamp <> '' update pd set pd.integrado=1 from inserted where pd.pdstamp=inserted.pdstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rech_insert ON rech WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO CH (chstamp,restamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,clbanco,clcheque,fref,tptit,contado,ollocal) select inserted.rechstamp,inserted.restamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.chdata,inserted.ousrdata,inserted.ousrdata,re.no,re.nome,re.estab,'RE',inserted.chvalor,inserted.echvalor,inserted.chvalorm,re.chmoeda,inserted.clbanco,inserted.clcheque,re.fref,inserted.tptit,re.contado, re.ollocal from inserted inner join re on re.restamp = inserted.restamp where re.cheque=1 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rech_delete ON rech WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM CH from deleted where ch.chstamp=deleted.rechstamp and ch.status = 1  update ch set   ch.origem = ''	, ch.restamp = ''  from deleted where ch.chstamp=deleted.rechstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tn_tentativa ON TN WITH ENCRYPTION FOR INSERT,UPDATE  AS SET NOCOUNT ON IF update (itentativa) BEGIN update tn set tn.tentativa=inserted.tentativa, tn.thora=inserted.thora,tn.tdata=inserted.tdata,tn.tquem=inserted.tquem from inserted where tn.oristamp=inserted.oristamp  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_nabdr_insert ON nabdr WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON			IF update(principal) AND (select COUNT(*)
									  from inserted
									  where principal=1)=1
				UPDATE nabdr
					set principal=0
				from inserted i
				where nabdr.nabdrstamp<>i.nabdrstamp
					and nabdr.ano=i.ano
					and i.principal=1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fmtx_update ON FMTX WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (desctx) BEGIN if update(desctx) BEGINupdate fmex set fmex.desctx=inserted.desctx from inserted where fmex.codtx=inserted.codtx update fmur set fmur.desctx=inserted.desctx from inserted where fmur.codtx=inserted.codtx update fmpe set fmpe.desctx=inserted.desctx from inserted where fmpe.codtx=inserted.codtx update fmec set fmec.desctx=inserted.desctx from inserted where fmec.codtx=inserted.codtx END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tn_inome ON TN WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (inome) BEGIN update tn set tn.nome=inserted.nome,tn.stamp=inserted.stamp,tn.telefone=inserted.telefone,tn.email=inserted.email,tn.no=inserted.no,tn.estab=inserted.estab,tn.contacto=inserted.contacto,tn.pquem=inserted.pquem from inserted where tn.oristamp=inserted.oristamp END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_clb_update ON CLB WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome) and (select count(*) from plf inner join deleted on plf.nmclb=deleted.nome)>0 begin update plf set plf.nmclb=inserted.nome from inserted inner join deleted on inserted.clbstamp=deleted.clbstamp where plf.nmclb=deleted.nome end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rerev_insert ON rerev WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF exists (select * from inserted where inserted.revog = 1)
begin
insert into ol (
olstamp,restamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,origem,
documento,cheque,data,dvalor,
entr,eentr,entrm,said,esaid,saidm,
moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,
site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select 
inserted.rerevstamp
,ol.restamp
,inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'REREV',
ol.documento,ol.cheque,inserted.revdata,inserted.revdata
,0
,0
,0
,ol.entr
,ol.eentr
,ol.entrm
,ol.moeda,ol.descricao,ol.olcodigo,ol.grupo,ol.sgrupo,ol.fref,ol.intid,ol.ccusto,ol.ncusto,ol.ollocal,ol.contado,ol.local,
ol.site,ol.pnome,ol.pno,ol.cxstamp,ol.cxusername,ol.ssstamp,ol.ssusername,ol.operext
from ol inner join inserted on ol.olstamp = inserted.restamp and inserted.revog = 1;
end 
ELSE
BEGIN
insert into ol (
olstamp,restamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,origem,
documento,cheque,data,dvalor,
entr,eentr,entrm,said,esaid,saidm,
moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,
site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select 
inserted.rerevstamp
,ol.restamp
,inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'REREV',
ol.documento,ol.cheque,inserted.revdata,inserted.revdata
,ol.entr
,ol.eentr
,ol.entrm
,0
,0
,0
,ol.moeda,ol.descricao,ol.olcodigo,ol.grupo,ol.sgrupo,ol.fref,ol.intid,ol.ccusto,ol.ncusto,ol.ollocal,ol.contado,ol.local,
ol.site,ol.pnome,ol.pno,ol.cxstamp,ol.cxusername,ol.ssstamp,ol.ssusername,ol.operext
from ol inner join inserted on ol.olstamp = inserted.restamp and inserted.revog = 0
and (select count(rerev.restamp) from rerev where rerev.restamp = inserted.restamp) > 1;
END			
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rerev_delete ON REREV WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM ol from deleted WHERE ol.olstamp = deleted.rerevstamp


update re
SET re.plano=t.plano, re.dplano = t.dplano, re.dinoplano = t.dinoplano 
, re.diaplano = t.diaplano, re.dilnoplano = t.dilnoplano 
, re.planoonline= t.planoonline
, re.dostamp=t.dostamp
, re.revogado=t.revog
from re inner join 
(select top 1 rerev.* from rerev inner join deleted on deleted.restamp = rerev.restamp order by rerev.ousrdata+rerev.ousrhora desc) as t
on re.restamp = t.restamp 				GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ol_insert ON ol WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO BA (bastamp,olstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,entrada,saida,eentrada,esaida,entradam,saidam,moeda,contado,intid )select inserted.olstamp,inserted.olstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'OL',inserted.cheque,inserted.documento,inserted.ccusto,inserted.ncusto,inserted.data,inserted.dvalor,inserted.fref,inserted.descricao,inserted.entr,inserted.said,inserted.eentr,inserted.esaid,inserted.entrm,inserted.saidm,inserted.moeda,inserted.contado,inserted.intidfrom inserted where inserted.contado>0 IF (select count(*) from inserted where inserted.local='C')>0 and not EXISTS(select fe.data from fe,inserted where inserted.local='C' and convert(char,fe.data,112)=convert(char,inserted.data,112))INSERT INTO FE (festamp,data,saldof,esaldof)          select data as festamp,                data as data,                Isnull((select top 1 saldof from fe where convert(char,fe.data,112)<convert(char,inserted.data,112) order by fe.data desc),0) as saldof,                Isnull((select top 1 esaldof from fe where convert(char,fe.data,112)<convert(char,inserted.data,112) order by fe.data desc),0) as esaldof          from inserted where inserted.local='C' and not EXISTS(select fe.data from fe,inserted where inserted.local='C' and convert(char,fe.data,112)=convert(char,inserted.data,112)) group by data update fe set fe.saldof=fe.saldof+(select sum(entr-said) from inserted where inserted.local='C'),fe.esaldof=fe.esaldof+(select sum(eentr-esaid) from inserted where inserted.local='C') from fe,inserted where inserted.local='C' and fe.data>=convert(char,inserted.data,112) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tbc_update ON tbc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE tbu set tbu.tgru=inserted.tgru,tbu.tsub=inserted.tsub from inserted where tbu.tcod=inserted.tcod UPDATE tmr set tmr.tgru=inserted.tgru,tmr.tsub=inserted.tsub from inserted where tmr.tcod=inserted.tcod 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fmun_update ON FMUN WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (descun) or update(eexame) or update(curso) or update(nhoras) BEGIN if update(descun) BEGINupdate fm set fm.descun=inserted.descun from inserted where fm.codun=inserted.codun update fmur set fmur.descun=inserted.descun from inserted where fmur.codun=inserted.codun  update fmin set fmin.descun=inserted.descun from inserted where fmin.codun=inserted.codun ENDif update(eexame) BEGINupdate fm set fm.eexame=inserted.eexame from inserted where fm.codun=inserted.codun ENDif update(curso) BEGINupdate fm set fm.curso=inserted.curso from inserted where fm.codun=inserted.codun ENDif update(nhoras) BEGINupdate fm set fm.nhoras=inserted.nhoras from inserted where fm.codun=inserted.codun END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ft2_insert ON ft2 WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select COUNT(*) from inserted where inserted.formapag='')>0
UPDATE ft2 SET formapag=1 
from inserted
WHERE ft2.ft2stamp=inserted.ft2stamp AND ft2.formapag=''GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_udefs_update ON UDEFS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (select count(*) from inserted inner join deleted on inserted.udefsstamp=deleted.udefsstamp where inserted.descricao<>deleted.descricao)>0 beginupdate gseln set gseln.descricao=inserted.descricao from inserted where gseln.udefsstamp=inserted.udefsstamp endGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ft2_deleted ON ft2 WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONif (select COUNT(*) from deleted where deleted.csupstamp<>'')>0
UPDATE csup SET csup.CSUPNMDOC = '',csup.CSUPADOC= '',csup.CSUPDATA=''
from csup
inner Join deleted on deleted.csupstamp=csup.csupstamp
WHERE  deleted.csupstamp<>''
IF (select count(*) from deleted inner join ft on ft.ftstamp = deleted.ft2stamp inner join td on td.ndoc=ft.ndoc and td.lancacli=1 
inner join cl on cl.no=deleted.c2no And cl.estab=deleted.c2estab 
where (ft.tipodoc=1 or ft.tipodoc=3) )>0 
UPDATE CL SET cl.acmfact=cl.acmfact-ft.ttiliq, cl.eacmfact=cl.eacmfact-ft.ettiliq from deleted 
inner join ft on ft.ftstamp = deleted.ft2stamp inner join td on td.ndoc=ft.ndoc and td.lancacli=1  and td.regrd = 0
where cl.no=deleted.c2no and ft.anulado=0 and (ft.tipodoc=1 or ft.tipodoc=3) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ba_insert ON ba WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE BL SET bl.saldo = bl.saldo + inserted.entrada-inserted.saida, bl.esaldo = bl.esaldo + inserted.eentrada-inserted.esaida, bl.saldom = bl.saldom + inserted.entradam-inserted.saidam, bl.rsaldo = bl.rsaldo + case when inserted.reco=1 then inserted.entrada-inserted.saida else 0 end, bl.ersaldo = bl.ersaldo + case when inserted.reco=1 then inserted.eentrada-inserted.esaida else 0 end, bl.rsaldom = bl.rsaldom + case when inserted.reco=1 then inserted.entradam-inserted.saidam else 0 end, bl.datasaldo = inserted.data  from INSERTED where bl.noconta=inserted.contadoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ba_delete ON ba WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE BL SET bl.saldo = bl.saldo-(deleted.entrada-deleted.saida), bl.esaldo = bl.esaldo-(deleted.eentrada-deleted.esaida), bl.saldom = bl.saldom-(deleted.entradam-deleted.saidam), bl.rsaldo = bl.rsaldo-(case when deleted.reco=1 then deleted.entrada-deleted.saida else 0 end), bl.ersaldo = bl.ersaldo-(case when deleted.reco=1 then deleted.eentrada-deleted.esaida else 0 end), bl.rsaldom = bl.rsaldom-(case when deleted.reco=1 then deleted.entradam-deleted.saidam else 0 end), bl.datasaldo = getdate() from deleted where bl.noconta=deleted.contadoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ug_update ON ug WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONupdate us set us.grupo = inserted.nome from inserted where us.ugstamp = inserted.ugstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_olbb_update ON olbb WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE OL set ol.transf=1,ol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.dvalor=inserted.dvalor,ol.ollocal=inserted.ollocal1,ol.contado=inserted.contado1,ol.data=inserted.data,ol.descricao=inserted.descricao,ol.said=inserted.valor,ol.esaid=inserted.evalor,ol.saidm=inserted.valorm1,ol.moeda=inserted.moeda1,ol.cheque=inserted.cheque,ol.documento=inserted.documento,ol.operext=inserted.operext,ol.cecope=inserted.cecopefrom inserted where inserted.contado1>0 and ol.olstamp=inserted.olbbstampUPDATE OL set ol.transf=1,ol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.dvalor=inserted.dvalor,ol.ollocal=inserted.ollocal2,ol.contado=inserted.contado2,ol.data=inserted.data,ol.descricao=inserted.descricao,ol.entr=inserted.valor,ol.eentr=inserted.evalor,ol.entrm=inserted.valorm2,ol.moeda=inserted.moeda2,ol.cheque=inserted.cheque,ol.documento=inserted.documento,ol.operext=inserted.operext,ol.cecope=inserted.cecopefrom inserted where inserted.contado2>0 and ol.olstamp=substring('C'+inserted.olbbstamp,1,25)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ctf_update ON CTF WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome) and (select count(*) from vcf inner join deleted on vcf.nmctf=deleted.nome)>0 begin update vcf set vcf.nmctf=inserted.nome from inserted inner join deleted on inserted.ctfstamp=deleted.ctfstamp where vcf.nmctf=deleted.nome endif update(nome) and (select count(*) from ac1 inner join deleted on ac1.nmctf=deleted.nome)>0 begin update ac1 set ac1.nmctf=inserted.nome from inserted inner join deleted on inserted.ctfstamp=deleted.ctfstamp where ac1.nmctf=deleted.nome end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_peps_update ON PEPS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE(eee)UPDATE pe SET pe.codlei20 = (case when inserted.eee = 1 then '0' else '3' end) FROM inserted WHERE pe.codnacnat = inserted.cm and pe.seportug = 1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_olbc_delete ON olbc WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.olbcstamp=deleted.olbcstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rt_insert ON RT WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF (select @@nestlevel) < 32 AND not EXISTS(select rt.ref,rt.refb from rt,inserted where rt.ref=inserted.refb and rt.refb=inserted.ref) insert into rt(rtstamp,ref,design,refb,ststamp,ousrinis,ousrdata,ousrhora,usrinis,usrdata,usrhora)  select substring(cast(newid() as char(36)),1,25),inserted.refb,st2.design,inserted.ref,st.ststamp,inserted.ousrinis,inserted.ousrdata,inserted.ousrhora,inserted.usrinis,inserted.usrdata,inserted.usrhora from inserted inner join st on inserted.ref=st.ref inner join st st2 on inserted.refb=st2.ref where not exists(select ref from rt where rt.ref=inserted.refb and rt.refb=inserted.ref)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rt_delete ON RT WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONIF (select @@nestlevel) < 32 AND EXISTS(select rt.ref from rt,deleted where rt.ref=deleted.refb and rt.refb=deleted.ref) delete from rt from deleted where rt.ref=deleted.refb and rt.refb=deleted.ref and EXISTS(select rt.ref from rt,deleted where rt.ref=deleted.refb and rt.refb=deleted.ref)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tit_insert ON tit WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rt_update ON RT WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF (select @@nestlevel) < 32 AND EXISTS(select rt.ref from rt,deleted where rt.ref=deleted.refb and rt.refb=deleted.ref) delete from rt from deleted where rt.ref=deleted.refb and rt.refb=deleted.ref and EXISTS(select rt.ref from rt,deleted where rt.ref=deleted.refb and rt.refb=deleted.ref)IF (select @@nestlevel) < 32 AND not EXISTS(select rt.ref,rt.refb from rt,inserted where rt.ref=inserted.refb and rt.refb=inserted.ref) insert into rt(rtstamp,ref,design,refb,ststamp,ousrinis,ousrdata,ousrhora,usrinis,usrdata,usrhora)  select substring(cast(newid() as char(36)),1,25),inserted.refb,st2.design,inserted.ref,st.ststamp,inserted.ousrinis,inserted.ousrdata,inserted.ousrhora,inserted.usrinis,inserted.usrdata,inserted.usrhora from inserted inner join st on inserted.ref=st.ref inner join st st2 on inserted.refb=st2.ref where not exists(select ref from rt where rt.ref=inserted.refb and rt.refb=inserted.ref)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tit_update ON tit WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bl_update ON bl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(conta) or update(banco)beginupdate ol set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where ol.contado=inserted.nocontaupdate bx set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where bx.contado=inserted.nocontaupdate olbb set ollocal1 = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where olbb.contado1=inserted.nocontaupdate olbb set ollocal2 = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where olbb.contado2=inserted.nocontaupdate olbc set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where olbc.contado=inserted.nocontaupdate op set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where op.contado=inserted.nocontaupdate ox set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where ox.contado=inserted.nocontaupdate ow set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where ow.contado=inserted.nocontaupdate re set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where re.contado=inserted.nocontaupdate rd set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where rd.contado=inserted.nocontaupdate po set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where po.contado=inserted.nocontaupdate pd set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where pd.contado=inserted.nocontaupdate vy set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where vy.contado=inserted.nocontaupdate vz set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where vz.contado=inserted.nocontaupdate pc set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where pc.contado=inserted.nocontaupdate le set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where le.contado=inserted.nocontaupdate lg set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where lg.contado=inserted.nocontaupdate lm set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where lm.contado=inserted.nocontaupdate lo set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where lo.contado=inserted.nocontaupdate lp set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where lp.contado=inserted.nocontaupdate lr set ollocal = convert(char(10),inserted.banco)+' '+convert(char(20),inserted.conta) from inserted where lr.contado=inserted.nocontaendGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_faca_delete ON faca WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE FAC SET fac.adiantado=fac.adiantado-(select sum(deleted.valor) from deleted where fac.facstamp=deleted.facstamp),fac.eadiantado=fac.eadiantado-(select sum(deleted.evalor) from deleted where fac.facstamp=deleted.facstamp) from deleted where fac.facstamp=deleted.facstampDELETE FROM OL from deleted where ol.facastamp=deleted.facastamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bo_update ON bo WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF EXISTS(SELECT 1 FROM bo2 as tempbo2 
INNER JOIN inserted ON inserted.bostamp COLLATE Latin1_General_CI_AI = tempbo2.bo2stamp COLLATE Latin1_General_CI_AI 
and tempbo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN ts on inserted.ndos = ts.ndos
INNER JOIN ts2 on ts.tsstamp COLLATE Latin1_General_CI_AI = ts2.ts2stamp COLLATE Latin1_General_CI_AI
WHERE ts2.assinaimp = 0
AND ts2.docsimport = 0
AND ts2.assinadoc = 1
AND inserted.obrano>0)
BEGIN
DECLARE @ErrorMessage NVARCHAR(2000) 
DECLARE @ErrorSeverity INT 
DECLARE @ErrorState INT 
SET @ErrorSeverity = 16
SET @ErrorState = 1
SELECT TOP 1 
@ErrorMessage = 'A assinatura digital no dossier interno ' + rtrim(inserted.nmdos) + ' n.º ' + cast(inserted.obrano as varchar(25)) +  ' está vazia. Por favor contacte o administrador do sistema.' 
FROM bo2 as tempbo2 
INNER JOIN inserted ON inserted.bostamp COLLATE Latin1_General_CI_AI = tempbo2.bo2stamp COLLATE Latin1_General_CI_AI 
and tempbo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN ts on inserted.ndos = ts.ndos
INNER JOIN ts2 on ts.tsstamp COLLATE Latin1_General_CI_AI = ts2.ts2stamp COLLATE Latin1_General_CI_AI
WHERE ts2.assinaimp = 0
AND ts2.docsimport = 0
AND ts2.assinadoc = 1
AND inserted.obrano>0
RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
ROLLBACK 
END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_clp_delete ON clp WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON		 	delete from cptclp where cptclp.clpstamp=(select deleted.clpstamp from deleted)		
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ow_insert ON OW WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF (select count(*) from inserted where inserted.ozstamp='' and inserted.postamp='' and inserted.restamp='' and ((inserted.ollinhas=1 and inserted.owliolcod=0) or inserted.ollinhas = 0)) > 0INSERT INTO OL (olstamp,owstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,entrm,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext) select inserted.owstamp,inserted.owstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'OW',inserted.docnome,inserted.cheque,inserted.data,inserted.dvalor,inserted.entr,inserted.eentr,inserted.said,inserted.esaid,inserted.entrm2,inserted.saidm2,inserted.moeda2,inserted.descricao,inserted.olcodigo,inserted.grupo,inserted.sgrupo,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.local,inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername,inserted.ssstamp,inserted.ssusername,inserted.operext from inserted where inserted.ozstamp='' and inserted.postamp='' and inserted.restamp='' and ((inserted.ollinhas=1 and inserted.owliolcod=0) or inserted.ollinhas = 0)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pr_insert ON pr WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (olstamp,prstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,intid,sgrupo,grupo,ollocal,olcodigo,moeda,saidm )select inserted.prstamp,inserted.prstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'PR',inserted.cheque,(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),inserted.ccusto,inserted.ncusto,inserted.oldata,inserted.data,inserted.fref,'Pagamento a '+inserted.nome,inserted.liquido,inserted.eliquido,inserted.contado,inserted.telocal,inserted.intid,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,inserted.moeda,inserted.mliquido from inserted where inserted.lol=1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facali_insert ON facali WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE FACA SET faca.valrec=faca.valrec+(select sum(inserted.valrec) from inserted where faca.facastamp=inserted.facastamp),faca.evalrec=faca.evalrec+(select sum(inserted.evalrec) from inserted where faca.facastamp=inserted.facastamp) from inserted where faca.facastamp=inserted.facastampUPDATE FACC SET facc.totaladirec=facc.totaladirec+(select sum(inserted.valrec) from inserted where facc.facstamp=inserted.facstamp),facc.etotaladirec=facc.etotaladirec+(select sum(inserted.evalrec) from inserted where facc.facstamp=inserted.facstamp) from inserted where facc.facstamp=inserted.facstampUPDATE FAC SET fac.adiantado=fac.adiantado-(select sum(inserted.valrec) from inserted where fac.facstamp=inserted.facstamp),fac.eadiantado=fac.eadiantado-(select sum(inserted.evalrec) from inserted where fac.facstamp=inserted.facstamp) from inserted where fac.facstamp=inserted.facstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pr_update ON PR WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE from OL from deleted where ol.prstamp = deleted.prstamp and deleted.prstamp not in (select inserted.prstamp from inserted where inserted.lol=1)INSERT INTO OL (olstamp,prstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,said,esaid,contado,local,intid,sgrupo,grupo,ollocal,olcodigo,moeda,saidm )select inserted.prstamp,inserted.prstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'PR',inserted.cheque,(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),inserted.ccusto,inserted.ncusto,inserted.oldata,inserted.data,inserted.fref,'Pagamento a '+inserted.nome,inserted.liquido,inserted.eliquido,inserted.contado,inserted.telocal,inserted.intid,inserted.sgrupo,inserted.grupo,inserted.ollocal,inserted.olcodigo,inserted.moeda,inserted.mliquido from inserted where inserted.lol=1 and inserted.prstamp not IN (select deleted.prstamp from deleted where deleted.lol=1)UPDATE OL SETol.usrinis=inserted.usrinis,ol.usrhora=inserted.usrhora,ol.usrdata=inserted.usrdata,ol.ousrinis=inserted.ousrinis,ol.ousrhora=inserted.ousrhora,ol.ousrdata=inserted.ousrdata,ol.origem='PR',ol.cheque=inserted.cheque,ol.documento=(case when inserted.contado > 0 then 'Cheque' else 'Dinheiro' end),ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.data=inserted.oldata,ol.dvalor=inserted.oldata,ol.fref=inserted.fref,ol.descricao='Pagamento a '+inserted.nome,ol.said=inserted.liquido,ol.esaid=inserted.eliquido,ol.contado=inserted.contado,ol.local=inserted.telocal,ol.intid=inserted.intid,ol.sgrupo=inserted.sgrupo,ol.grupo=inserted.grupo,ol.ollocal=inserted.ollocal,ol.olcodigo=inserted.olcodigo,ol.moeda=inserted.moeda,ol.saidm=inserted.mliquido from inserted where inserted.lol=1 and inserted.prstamp=ol.prstamp and inserted.prstamp IN (select deleted.prstamp from deleted where deleted.lol=1)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facali_update ON facali WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE FACA SET faca.valrec=faca.valrec-(select sum(deleted.valrec) from deleted where faca.facastamp=deleted.facastamp),faca.evalrec=faca.evalrec-(select sum(deleted.evalrec) from deleted where faca.facastamp=deleted.facastamp) from deleted where faca.facastamp=deleted.facastampUPDATE FACC SET facc.totaladirec=facc.totaladirec-(select sum(deleted.valrec) from deleted where facc.facstamp=deleted.facstamp),facc.etotaladirec=facc.etotaladirec-(select sum(deleted.evalrec) from deleted where facc.facstamp=deleted.facstamp) from deleted where facc.facstamp=deleted.facstampUPDATE FAC SET fac.adiantado=fac.adiantado+(select sum(deleted.valrec) from deleted where fac.facstamp=deleted.facstamp),fac.eadiantado=fac.eadiantado+(select sum(deleted.evalrec) from deleted where fac.facstamp=deleted.facstamp) from deleted where fac.facstamp=deleted.facstampUPDATE FACA SET faca.valrec=faca.valrec+(select sum(inserted.valrec) from inserted where faca.facastamp=inserted.facastamp),faca.evalrec=faca.evalrec+(select sum(inserted.evalrec) from inserted where faca.facastamp=inserted.facastamp) from inserted where faca.facastamp=inserted.facastampUPDATE FACC SET facc.totaladirec=facc.totaladirec+(select sum(inserted.valrec) from inserted where facc.facstamp=inserted.facstamp),facc.etotaladirec=facc.etotaladirec+(select sum(inserted.evalrec) from inserted where facc.facstamp=inserted.facstamp) from inserted where facc.facstamp=inserted.facstampUPDATE FAC SET fac.adiantado=fac.adiantado-(select sum(inserted.valrec) from inserted where fac.facstamp=inserted.facstamp),fac.eadiantado=fac.eadiantado-(select sum(inserted.evalrec) from inserted where fac.facstamp=inserted.facstamp) from inserted where fac.facstamp=inserted.facstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_prre_insert ON prre WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE DL SET DL.PROCESS=1,DL.PROCDATA=inserted.DTREF from inserted inner join dl on inserted.dlstamp=dl.dlstamp where inserted.dlstamp<>''
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facc_insert ON facc WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from inserted where inserted.etotalcom<>0 or inserted.totalcom<>0)>0INSERT INTO OL (olstamp,faccstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,said,esaid,moeda,descricao,olcodigo,grupo,sgrupo,ollocal,contado,local) select inserted.faccstamp,inserted.faccstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'FACA',inserted.cessao,convert(char(15),inserted.nref),inserted.data,inserted.data,inserted.totalcom,inserted.etotalcom,'PTE ou EURO',inserted.contrato,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.ollocal,inserted.contado,inserted.telocal from inserted inner join oc on oc.olcodigo=inserted.olcodigo where inserted.totalcom<>0 or inserted.etotalcom<>0
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_prre_delete ON prre WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE DL SET DL.PROCESS=0,DL.PROCDATA='' from deleted inner join dl on deleted.dlstamp=dl.dlstamp where deleted.dlstamp<>''
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_opc_delete ON opc WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete from ocp FROM deleted inner join bo2 on bo2.processo=deleted.processo inner join bo on bo.bostamp=bo2.bo2stamp inner join ts on ts.ndos=bo.ndos and ts.pedidopv=1 inner join bi on bi.bostamp=bo.bostamp where ocp.orilstamp=bi.bistamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facc_update ON facc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.faccstamp=deleted.faccstamp if (select count(*) from inserted where inserted.etotalcom<>0 or inserted.totalcom<>0)>0INSERT INTO OL (olstamp,faccstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,said,esaid,moeda,descricao,olcodigo,grupo,sgrupo,ollocal,contado,local) select inserted.faccstamp,inserted.faccstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'FACA',inserted.cessao,convert(char(15),inserted.nref),inserted.data,inserted.data,inserted.totalcom,inserted.etotalcom,'PTE ou EURO',inserted.contrato,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.ollocal,inserted.contado,inserted.telocal from inserted inner join oc on oc.olcodigo=inserted.olcodigo where inserted.totalcom<>0 or inserted.etotalcom<>0
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rct_update ON RCT WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(idrct) and (select count(*) from srt inner join deleted on srt.idrct=deleted.idrct)>0 begin update srt set srt.idrct=inserted.idrct from inserted inner join deleted on inserted.rctstamp=deleted.rctstamp where srt.idrct=deleted.idrct endif update(idrct) and (select count(*) from etr inner join deleted on etr.idrct=deleted.idrct)>0 begin update etr set etr.idrct=inserted.idrct from inserted inner join deleted on inserted.rctstamp=deleted.rctstamp where etr.idrct=deleted.idrct end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftcc_delete ON ftcc WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete from CH from deleted where ch.ftccstamp=deleted.ftccstamp delete from CC from deleted where cc.ftccstamp=deleted.ftccstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ox_insert ON ox WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (transf,olstamp,oxstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,intid,ccusto,ncusto,dvalor,local,ollocal,contado,data,descricao,entr,eentr,entrm,moeda,olcodigo,grupo,sgrupo,cheque,documento )select 1,inserted.oxstamp,inserted.oxstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'OX',inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.dvalor,case when inserted.contado>0 then 'B' else 'C' end,inserted.ollocal,inserted.contado,inserted.data,inserted.oxdesc,inserted.valor,inserted.evalor,inserted.valorm,inserted.moeda,'Trf','Transferências','Transferências',inserted.notalao,'Talão de Depósito'from inserted where inserted.contado>0 INSERT INTO OL (transf,olstamp,oxstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,intid,ccusto,ncusto,dvalor,local,ollocal,contado,data,descricao,said,esaid,saidm,moeda,olcodigo,grupo,sgrupo,cheque,documento )select 1,substring('C'+inserted.oxstamp,1,25),inserted.oxstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'OX',inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.dvalor,case when inserted.ocontado>0 then 'B' else 'C' end,inserted.olorigem,inserted.ocontado,inserted.data,inserted.oxdesc,inserted.valor,inserted.evalor,inserted.valorm1,inserted.moeda1,'Trf','Transferências','Transferências',inserted.notalao,'Talão de Depósito'from inserted where inserted.contado>0 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oy_insert ON oy WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE CH set ch.jadepo=1,ch.dataoy=ox.data from inserted inner join ox on ox.oxstamp=inserted.oxstamp where ch.chstamp=inserted.chstamp  UPDATE ch  SET ch.status =  (select top 1 cm  from ( select chl.cm, chl.ousrdata, chl.ousrhora     from chl INNER JOIN ch ON ch.chstamp = chl.chstamp    inner join inserted on inserted.chstamp = ch.chstamp    union all  select ox.cm, oy.ousrdata, oy.ousrhora  from ox inner join oy on ox.oxstamp = oy.oxstamp  inner join inserted on inserted.chstamp = oy.chstamp	  )t  order by ousrdata+ousrhora desc)   from inserted, ch where inserted.chstamp = ch.chstamp  GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_e1ca_update ON e1ca WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIf update(e1cano) UPDATE E2 SET e2.e1cano=inserted.e1cano  from inserted where e2.e1castamp=inserted.e1castampIf update(e1cano) UPDATE E1LI SET e1li.e1cano=inserted.e1cano  from inserted where e1li.e1castamp=inserted.e1castampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_em_delete ON EM WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate cl set cl.emno=0,cl.eem=0 from deleted where cl.emno=deleted.noupdate ag set ag.emno=0,ag.eem=0 from deleted where ag.emno=deleted.noupdate cts set cts.noem=0,cts.nomeem='' from deleted where cts.noem=deleted.no
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftreg_update ON ftreg WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONdeclare @m_debfm numeric(19,6)declare @m_ftregstamp varchar(25)declare @m_vmoeda numeric(19,6)select @m_ftregstamp=(select top 1 deleted.ftregstamp from deleted group by deleted.ftregstamp)select @m_vmoeda=(select top 1 sum(inserted.vmoeda) from inserted group by inserted.ftregstamp)select @m_debfm=(select sum(ftreg.vmoeda) from ftreg where ftreg.ftregstamp=@m_ftregstamp)-@m_vmoedaUPDATE CC set cc.debf=(case when cc.debf-round(deleted.vciva,0)<0 then 0 else cc.debf-round(deleted.vciva,0) end) , cc.edebf=(case when cc.edebf-round(deleted.evciva,2 )<0 then 0 else cc.edebf-round(deleted.evciva,2 ) end), cc.debfm=(case when @m_debfm<0 then 0 else @m_debfm end), cc.ultdoc='' from deleted where cc.ftstamp=deleted.ftregstampUPDATE CC set cc.debf=(case when cc.debf+round(inserted.vciva,0)>=cc.deb then cc.deb else cc.debf+round(inserted.vciva,0) end), cc.edebf=(case when cc.edebf+round(inserted.evciva,2)>=cc.edeb then cc.edeb else cc.edebf+round(inserted.evciva,2) end), cc.debfm=(case when ROUND(cc.debfm+inserted.vmoeda,2)>=cc.debm then cc.debm else cc.debfm+inserted.vmoeda end), cc.ultdoc=inserted.ultdoc from inserted where cc.ftstamp=inserted.ftregstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oz_delete ON OZ WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.ozstamp=deleted.ozstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_e1li_update ON e1li WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE E2 SET e2.e1lino=inserted.e1lino,e2.lidesc=inserted.lidesc,e2.numerico=inserted.numerico  from inserted where e2.e1listamp=inserted.e1listampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ob_delete ON OB WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDelete from ifl from deleted where ifl.oritable='OB' and ifl.recstamp=deleted.obstamp and ifl.uniqueid='imagem'
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vi_delete ON VI WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate em set em.proxaccao= isnull((select top 1 vi.resumo from vi where vi.no=deleted.no and vi.lsitu=0  order by data,hora ),''), em.proxvisita=isnull((select top 1 vi.data from vi where vi.no=deleted.no and vi.lsitu=0  order by data,hora ), ''), em.proxhora=isnull((select top 1 vi.hora from vi where vi.no=deleted.no and vi.lsitu=0  order by data,hora ),'') from deleted where em.no=deleted.no 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vy_delete ON vy WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from OD from deleted where od.vystamp=deleted.vystampUpdate pn set pn.proces=0, pn.VALORPROC=pn.VALORPROC-deleted.vendas ,pn.eVALORPROC=pn.eVALORPROC-deleted.evendas from deleted inner join vydoc on vydoc.vystamp=deleted.vystamp inner join re (nolock) on vydoc.oristamp=re.restamp inner join rl (nolock) on re.restamp=rl.restamp inner join cc (nolock) on rl.ccstamp=cc.ccstamp inner join pn (nolock) on cc.ftstamp=pn.ftstamp DELETE from vydoc from deleted where vydoc.vystamp=deleted.vystampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tta_delete ON tta WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE TFA SET TFA.hreal=TFA.hreal-deleted.hreal,TFA.hprev=TFA.hprev-deleted.hprev from DELETED where TFA.tfastamp=deleted.tfastamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vy_update ON VY WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE FROM OD FROM deleted where deleted.vystamp=od.vystamp INSERT INTO OD (vystamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,data,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local) select inserted.vystamp,inserted.vystamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'VY',inserted.demi,inserted.valornp,inserted.evalornp,'Comissões do Vendedor: '+rtrim(inserted.vendnm)+', nº '+convert(char,inserted.vendedor),inserted.olcodigo,inserted.grupo,inserted.sgrupo,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.local from inserted where (inserted.valornp<>0 and inserted.evalornp<>0) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pc_insert ON PC WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128) 
SELECT @Cinfo = Context_Info()
IF @Cinfo = 0x12345
RETURN
UPDATE pc set
codiva=ISNULL((select top 1 pc2.codiva 
from pc pc2 
where pc2.conta<pc.conta and LEFT(pc.conta,LEN(RTRIM(pc2.conta)))=pc2.conta and pc2.ano=pc.ano 
order by pc2.conta desc),0)
from inserted
where pc.pcstamp=inserted.pcstamp and pc.codiva=0
UPDATE pc set
codis=ISNULL((select top 1 pc2.codis 
from pc pc2 
where pc2.conta<pc.conta and LEFT(pc.conta,LEN(RTRIM(pc2.conta)))=pc2.conta and pc2.ano=pc.ano 
order by pc2.conta desc),'')
from inserted
where pc.pcstamp=inserted.pcstamp and pc.codis=''
-- &&84394
declare @radical varchar(15)
SET @radical = (SELECT para1.valor from para1 (nolock)	where UPPER(para1.descricao) = 'ML_OBRIGAOL')
IF RTRIM(@radical)<>''
BEGIN
update pc 
set pc.obrigaol = 1

from inserted 
where (pc.pcstamp = inserted.pcstamp 
and  inserted.conta like rtrim(@radical)+'%') and (inserted.integracao = 0
)
END
-- &&84394GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vz_insert ON vz WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO OL (olstamp,vzstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,operext,cecope) select inserted.vzstamp,inserted.vzstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'VZ',inserted.documento,inserted.adoc,inserted.procdata,inserted.dvalor,inserted.total,inserted.etotal,'Pagamento de comissões do Vendedor: '+rtrim(inserted.nome)+', nº '+convert(char,inserted.no),inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.telocal,inserted.operext,inserted.cecope from inserted inner join oc on oc.olcodigo=inserted.olcodigo where inserted.process=1 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cb_insert ON CB WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO CBPAISES (pais, moeda, cbpaisesstamp, unisg, unipl, centsg, centpl, zonaeuro)
SELECT pais, moeda, left(replace(cast(newid() as varchar(100)),'-',''),24), unisg, unipl, centsg, centpl, zonaeuro FROM inserted 
WHERE NOT EXISTS (select * from cbpaises where cbpaises.pais = inserted.pais and cbpaises.moeda = inserted.moeda) 
IF EXISTS(SELECT 1 FROM inserted WHERE ecambio = 0 AND ecambio2 = 0)			
BEGIN 
SET Context_Info 0x12345

UPDATE cb
SET cb.ecambio = 1/CASE WHEN inserted.ecambioinvertido<>0 THEN inserted.ecambioinvertido ELSE 1 END 
, cb.ecambio2 =  1/CASE WHEN inserted.ecambioinvertido2<>0 THEN inserted.ecambioinvertido2 ELSE 1 END 
, cb.cambio = 1/CASE WHEN inserted.cambioinvertido<>0 THEN inserted.cambioinvertido ELSE 1 END 
, cb.cambio2 =  1/CASE WHEN inserted.cambioinvertido2<>0 THEN inserted.cambioinvertido2 ELSE 1 END 					
FROM cb
INNER JOIN inserted ON cb.cbstamp = inserted.cbstamp	
WHERE inserted.ecambio = 0 AND inserted.ecambio2 = 0

SET Context_Info 0x 				
END

IF EXISTS(SELECT 1 FROM inserted WHERE ecambioinvertido = 0 AND ecambioinvertido2 = 0)			
BEGIN 
SET Context_Info 0x12345

UPDATE cb
SET cb.ecambioinvertido = 1/CASE WHEN inserted.ecambio<>0 THEN inserted.ecambio ELSE 1 END 
, cb.ecambioinvertido2 = 1/CASE WHEN inserted.ecambio2<>0 THEN inserted.ecambio2 ELSE 1 END 
, cb.cambioinvertido = 1/CASE WHEN inserted.cambio<>0 THEN inserted.cambio ELSE 1 END 
, cb.cambioinvertido2 = 1/CASE WHEN inserted.cambio2<>0 THEN inserted.cambio2 ELSE 1 END 					
FROM cb
INNER JOIN inserted ON cb.cbstamp = inserted.cbstamp	
WHERE inserted.ecambioinvertido = 0 AND inserted.ecambioinvertido2 = 0

SET Context_Info 0x 				
END			GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_foma_insert ON foma WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO MA (mastamp,fomastamp,fostamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,flnome,flno,flestab,flnmdoc,fladoc,fldata,casa,user1,user2,user3,user4,situacao,noarm,armdatain,obs,foorigem  )select inserted.fomastamp,inserted.fomastamp,inserted.fostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.nome,inserted.no,inserted.estab,inserted.nmdoc,inserted.adoc,inserted.data,1,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.situacao, inserted.armazem,inserted.data,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.data,121)+')' end,'FO' from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where cm1.debito=0 UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='FO',ma.bostampv=inserted.fostamp,ma.ftnmdoc=inserted.nmdoc,ma.ftfno=inserted.adoc,ma.ftfdata=inserted.data, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.data,121)+')' end from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where inserted.mastamp=ma.mastamp  and cm1.debito=1 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tmr_update ON tmr WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET tpo.etentr=tpo.etentr-deleted.eentr, tpo.etsaid=tpo.etsaid-deleted.esaid, tpo.tentr=tpo.tentr-deleted.entr, tpo.tsaid=tpo.tsaid-deleted.said from deleted where tpo.tpostamp=deleted.tpostamp UPDATE TPO SET tpo.etentr=tpo.etentr+inserted.eentr, tpo.etsaid=tpo.etsaid+inserted.esaid, tpo.tentr=tpo.tentr+inserted.entr, tpo.tsaid=tpo.tsaid+inserted.said, TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where tpo.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_foma_update ON foma WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.ousrinis=inserted.ousrinis,ma.ousrhora=inserted.ousrhora,ma.ousrdata=inserted.ousrdata,ma.serie=inserted.serie,ma.serie2=inserted.serie2,ma.marca=inserted.marca,ma.maquina=inserted.maquina,ma.tipo=inserted.tipo,ma.fref=inserted.fref,ma.ccusto=inserted.ccusto,ma.ncusto=inserted.ncusto,ma.noarm=inserted.armazem,ma.armdatain=inserted.data, ma.user1=inserted.user1,ma.user2=inserted.user2,ma.user3=inserted.user3,ma.user4=inserted.user4,ma.ref=inserted.ref,ma.design=inserted.design,ma.situacao=inserted.situacao,ma.obs=case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.data,121)+')' end, ma.flno=inserted.no,ma.flnome=inserted.nome,ma.flestab=inserted.estab,ma.flnmdoc=inserted.nmdoc,ma.foorigem='FO',ma.fostamp=inserted.fostamp,ma.fldata=inserted.data,ma.fladoc=inserted.adoc from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where inserted.fomastamp=ma.fomastamp and cm1.debito=0 INSERT INTO MA (mastamp,fomastamp,fostamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,flnome,flno,flestab,flnmdoc,fladoc,fldata,casa,noarm,armdatain,user1,user2,user3,user4,situacao,obs,foorigem )select inserted.fomastamp,inserted.fomastamp,inserted.fostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto, inserted.nome,inserted.no,inserted.estab,inserted.nmdoc,inserted.adoc,inserted.data,1,inserted.armazem,inserted.data,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.situacao, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.data,121)+')' end,'FO' from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where not exists (select fomastamp from ma where ma.fomastamp=inserted.fomastamp ) and cm1.debito=0 UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.bostampv=inserted.fostamp,ma.indisponivel=1,ma.ftorigem='FO',ma.ftnmdoc=inserted.nmdoc,ma.ftfno=inserted.adoc,ma.ftfdata=inserted.data, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.data,121)+')' end from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where inserted.mastamp=ma.mastamp  and cm1.debito=1 UPDATE MA SETma.casa=1,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.data,121))+4,datalength(ma.obs)), ma.indisponivel=0,ma.ftorigem='',ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='' from deleted inner join fo on deleted.fostamp=fo.fostamp inner join cm1 on cm1.cm=fo.doccode where deleted.mastamp=ma.mastamp and cm1.debito=1 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_PD_delete ON PD WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM FC from deleted where FC.pdstamp=deleted.pdstamp
DELETE FROM OL from deleted where ol.pdstamp=deleted.pdstamp 

IF exists (select 1 from deleted where deleted.cheque <> '')				
UPDATE chf
SET deststamp = ''
, destino = ''
from deleted inner join chf on deleted.chfstamp = chf.chfstamp 
where deleted.cheque <> ''		
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ol_update ON ol WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE from BA from inserted where ba.olstamp = inserted.olstamp and inserted.contado=0 UPDATE BA SETba.usrinis=inserted.usrinis,ba.usrhora=inserted.usrhora,ba.usrdata=inserted.usrdata,ba.ousrinis=inserted.ousrinis,ba.ousrhora=inserted.ousrhora,ba.ousrdata=inserted.ousrdata,ba.origem='OL',ba.cheque=inserted.cheque,ba.documento=inserted.documento,ba.ccusto=inserted.ccusto,ba.ncusto=inserted.ncusto,ba.data=inserted.data,ba.dvalor=inserted.dvalor,ba.fref=inserted.fref,ba.descricao=inserted.descricao,ba.entrada=inserted.entr,ba.saida=inserted.said,ba.eentrada=inserted.eentr,ba.esaida=inserted.esaid,ba.entradam=inserted.entrm,ba.saidam=inserted.saidm,ba.moeda=inserted.moeda,ba.contado=inserted.contado,ba.intid=inserted.intid from inserted where inserted.contado>0 and inserted.olstamp=ba.olstamp  INSERT INTO BA (bastamp,olstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,cheque,documento,ccusto,ncusto,data,dvalor,fref,descricao,entrada,saida,eentrada,esaida,entradam,saidam,moeda,contado,intid )select inserted.olstamp,inserted.olstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'OL',inserted.cheque,inserted.documento,inserted.ccusto,inserted.ncusto,inserted.data,inserted.dvalor,inserted.fref,inserted.descricao,inserted.entr,inserted.said,inserted.eentr,inserted.esaid,inserted.entrm,inserted.saidm,inserted.moeda,inserted.contado,inserted.intid from inserted where inserted.contado>0 and not exists (select olstamp from ba where ba.olstamp=inserted.olstamp ) IF (select count(*) from inserted where inserted.local='C')>0 and not EXISTS(select fe.data from fe,inserted where inserted.local='C' and convert(char,fe.data,112)=convert(char,inserted.data,112))INSERT INTO FE (festamp,data,saldof,esaldof)          select data as festamp,                data as data,                Isnull((select top 1 Isnull(saldof,0) from fe where convert(char,fe.data,112)<convert(char,inserted.data,112) order by fe.data desc),0) as saldof,                Isnull((select top 1 Isnull(esaldof,0) from fe where convert(char,fe.data,112)<convert(char,inserted.data,112) order by fe.data desc),0) as esaldof          from inserted where inserted.local='C' and not EXISTS(select fe.data from fe,inserted where inserted.local='C' and convert(char,fe.data,112)=convert(char,inserted.data,112)) group by data update fe set fe.saldof=fe.saldof+(select sum(entr-said) from inserted where inserted.local='C'),fe.esaldof=fe.esaldof+(select sum(eentr-esaid) from inserted where inserted.local='C') from fe,inserted where inserted.local='C' and fe.data>=convert(char,inserted.data,112) IF (select count(*) from deleted where deleted.local='C')>0 and not EXISTS(select fe.data from fe,deleted where deleted.local='C' and convert(char,fe.data,112)=convert(char,deleted.data,112))INSERT INTO FE (festamp,data,saldof,esaldof)          select data as festamp,                data as data,                Isnull((select top 1 Isnull(saldof,0) from fe where convert(char,fe.data,112)<convert(char,deleted.data,112) order by fe.data desc),0) as saldof,                Isnull((select top 1 Isnull(esaldof,0) from fe where convert(char,fe.data,112)<convert(char,deleted.data,112) order by fe.data desc),0) as esaldof          from deleted where deleted.local='C' and not EXISTS(select fe.data from fe,deleted where deleted.local='C' and convert(char,fe.data,112)=convert(char,deleted.data,112)) group by data update fe set fe.saldof=fe.saldof-(select sum(entr-said) from deleted where deleted.local='C'),fe.esaldof=fe.esaldof-(select sum(eentr-esaid) from deleted where deleted.local='C') from fe,deleted where deleted.local='C' and fe.data>=convert(char,deleted.data,112) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ow_delete ON OW WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.owstamp=deleted.owstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cev_update ON cev WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON		-- Verifica se ocorreram alterações em algum dos contadores a partir de CEV, no que diz respeito às configurações
		-- ou ao nome do evento
		IF(	UPDATE(cont1nm) OR UPDATE(cont1chk) OR
			UPDATE(cont2nm) OR UPDATE(cont2chk) OR
			UPDATE(cont3nm) OR UPDATE(cont3chk) OR
			UPDATE(cont4nm) OR UPDATE(cont4chk) OR
			UPDATE(cont5nm) OR UPDATE(cont5chk) OR
			UPDATE(cont6nm) OR UPDATE(cont6chk) OR
			UPDATE(cont7nm) OR UPDATE(cont7chk) OR
			UPDATE(cont8nm) OR UPDATE(cont8chk) OR
			UPDATE(cont9nm) OR UPDATE(cont9chk) OR
			UPDATE(evento)
		)
		BEGIN
			-- Caso tenham existido alterações às configurações dos contadores a partir de CEV, actualiza
			-- todas as inscrições relacionadas com este evento com as respectivas alterações efectuadas
			UPDATE CEVI
			SET	CEVI.cont1nmi = INSERTED.cont1nm,
				CEVI.cont2nmi = INSERTED.cont2nm,
				CEVI.cont3nmi = INSERTED.cont3nm,
				CEVI.cont4nmi = INSERTED.cont4nm,
				CEVI.cont5nmi = INSERTED.cont5nm,
				CEVI.cont6nmi = INSERTED.cont6nm,
				CEVI.cont7nmi = INSERTED.cont7nm,
				CEVI.cont8nmi = INSERTED.cont8nm,
				CEVI.cont9nmi = INSERTED.cont9nm,
				CEVI.cont1chk = INSERTED.cont1chk,
				CEVI.cont2chk = INSERTED.cont2chk,
				CEVI.cont3chk = INSERTED.cont3chk,
				CEVI.cont4chk = INSERTED.cont4chk,
				CEVI.cont5chk = INSERTED.cont5chk,
				CEVI.cont6chk = INSERTED.cont6chk,
				CEVI.cont7chk = INSERTED.cont7chk,
				CEVI.cont8chk = INSERTED.cont8chk,
				CEVI.cont9chk = INSERTED.cont9chk,
				CEVI.cev = INSERTED.evento
			FROM INSERTED
			WHERE CEVI.cevstamp = INSERTED.cevstamp
		END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_facl_update ON facl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE CC SET cc.faccstamp='',cc.faclstamp='' from deleted where cc.ccstamp=deleted.ccstampUPDATE FACC SET facc.totalrec=facc.totalrec-(select sum(deleted.escrec) from deleted where facc.facstamp=deleted.facstamp),facc.etotalrec=facc.etotalrec-(select sum(deleted.erec) from deleted where facc.facstamp=deleted.facstamp) from deleted where facc.facstamp=deleted.facstampUPDATE FAC SET fac.cessionado=fac.cessionado-(select sum(deleted.escval-deleted.escrec) from deleted where fac.facstamp=deleted.facstamp),fac.ecessionado=fac.ecessionado-(select sum(deleted.eval-deleted.erec) from deleted where fac.facstamp=deleted.facstamp) from deleted where fac.facstamp=deleted.facstampUPDATE FAL SET fal.cessionado=fal.cessionado-(select sum(deleted.escval-deleted.escrec) from deleted where fal.falstamp=deleted.falstamp),fal.ecessionado=fal.ecessionado-(select sum(deleted.eval-deleted.erec) from deleted where fal.falstamp=deleted.falstamp) from deleted where fal.falstamp=deleted.falstampUPDATE CC SET cc.faccstamp=inserted.faccstamp,cc.faclstamp=inserted.faclstamp from inserted where cc.ccstamp=inserted.ccstampUPDATE FACC SET facc.totalrec=facc.totalrec+(select sum(inserted.escrec) from inserted where facc.facstamp=inserted.facstamp),facc.etotalrec=facc.etotalrec+(select sum(inserted.erec) from inserted where facc.facstamp=inserted.facstamp) from inserted where facc.facstamp=inserted.facstampUPDATE FAC SET fac.cessionado=fac.cessionado+(select sum(inserted.escval-inserted.escrec) from inserted where fac.facstamp=inserted.facstamp),fac.ecessionado=fac.ecessionado+(select sum(inserted.eval-inserted.erec) from inserted where fac.facstamp=inserted.facstamp) from inserted where fac.facstamp=inserted.facstampUPDATE FAL SET fal.cessionado=fal.cessionado+(select sum(inserted.escval-inserted.escrec) from inserted where fal.falstamp=inserted.falstamp),fal.ecessionado=fal.ecessionado+(select sum(inserted.eval-inserted.erec) from inserted where fal.falstamp=inserted.falstamp) from inserted where fal.falstamp=inserted.falstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vz_delete ON vz WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL from deleted where ol.vzstamp=deleted.vzstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fofc_insert ON fofc WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO FC (fcstamp,fostamp,fofcstamp,obs,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,tpdesc,tpstamp,datalc,no,nome,cm,cmdesc,origem,cred,ecred,deb,edeb,credm,debm,aprovado,nmaprov,dtaprov,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,formapag,situacao,moeda,adoc,dataven,fref,cambiofixo,ccusto,ncusto,intid,tipo,estab,pais,zona,virs,evirs) select inserted.fofcstamp,inserted.fostamp,inserted.fofcstamp,inserted.obs,fo.usrdata,fo.usrhora,fo.usrinis,fo.ousrdata,fo.ousrhora,fo.ousrinis,fo.tpdesc,fo.tpstamp,fo.data,fo.no,fo.nome,cm1.fomulti,cm1.fomultidesc,'FO',case when cm1.debito=1 then 0 else inserted.cred end,case when cm1.debito=1 then 0 else inserted.ecred end,case when cm1.debito=1 then inserted.cred else 0 end,case when cm1.debito=1 then inserted.ecred else 0 end,case when cm1.debito=1 then 0 else inserted.credm end,case when cm1.debito=1 then inserted.credm else 0 end,fo.aprovado,fo.nmaprov,fo.dtaprov,inserted.ivav1,inserted.ivav2,inserted.ivav3,inserted.ivav4,inserted.ivav5,inserted.ivav6,inserted.ivav7,inserted.ivav8,inserted.ivav9, inserted.eivav1,inserted.eivav2,inserted.eivav3,inserted.eivav4,inserted.eivav5,inserted.eivav6,inserted.eivav7,inserted.eivav8,inserted.eivav9, inserted.formapag,'1',fo.moeda,fo.adoc,inserted.dataven,fo.fref,fo.cambiofixo,fo.ccusto,fo.ncusto,fo.intid,fo.tipo,fo.estab,fo.pais,fo.zona,inserted.virs,inserted.evirs from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on cm1.cm=fo.doccode where cm1.folanfc=1  GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_hs_delete ON HS WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUpdate abs set abs.anulada = 0, abs.classif = 0, abs.abatefp = (case when (select count(*) from absfp where absfp.absstamp = abs.absstamp) > 0 then 1 else 0 end) from deleted where deleted.ntipo = 2 and abs.classif = 1 and abs.absstamp=deleted.absstamp and deleted.absstamp not in (select hs.absstamp from hs where hs.no = deleted.no and hs.hsstamp <> deleted.hsstamp)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_hbc_update ON HBC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome) and (select count(*) from cv1 inner join deleted on cv1.nome=deleted.nome)>0 begin update cv1 set cv1.nome=inserted.nome from inserted inner join deleted on inserted.hbcstamp=deleted.hbcstamp where cv1.nome=deleted.nome end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mhct_insert ON mhct WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from inserted where inserted.valor>0)>0 BEGIN UPDATE MACT SETmact.usrinis=inserted.usrinis,mact.usrhora=inserted.usrhora,mact.usrdata=inserted.usrdata,mact.valor=case when mact.valor<inserted.valor then inserted.valor else mact.valor end, mact.data=case when mact.data<inserted.data then inserted.data else mact.data end from inserted inner join mact on mact.mactstamp=inserted.mactstamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pos_delete ON pos WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE from POSSS from deleted where posss.posstamp=deleted.posstamp DELETE from POSCX from deleted where poscx.posstamp=deleted.posstamp DELETE from POSTD from deleted where postd.posstamp=deleted.posstamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER ON
GO

create FUNCTION fn_iBERIA_Calc_PL(@fcstamp as CHAR(25), @data_fim as datetime)
RETURNS NUMERIC(20,6)
with encryption
AS
BEGIN
	DECLARE @mySaldo NUMERIC(20,6)

	set @mySaldo = 	(			
			select
				sum(pl.erec)
			from pl (nolock)
				join po (nolock) on	po.postamp=pl.postamp
							and po.process=1
							and po.procdata<=@data_fim
							and pl.fcstamp=@fcstamp		
			)

	RETURN ISNULL(@mySaldo,0)
end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sgt_update ON SGT WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE(tam) update sx set sx.tam=inserted.tam from inserted,deleted where sx.ref=inserted.ref and sx.tam=deleted.tam GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oci_delete ON oci WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OCP FROM deleted where ocp.ocistamp=deleted.ocistamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tbu_delete ON tbu WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE TPO SET tpo.ebentr=tpo.ebentr-deleted.eentr, tpo.ebsaid=tpo.ebsaid-deleted.esaid, tpo.bentr=tpo.bentr-deleted.entr, tpo.bsaid=tpo.bsaid-deleted.said from deleted where tpo.tpostamp=deleted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fr_delete ON FR WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.frstamp=deleted.frstampUPDATE DL SET DL.PROCESS=0,DL.PROCDATA='' from deleted inner join dl on deleted.dlstamp=dl.dlstamp where deleted.dlstamp<>''
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tbu_update ON tbu WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET tpo.ebentr=tpo.ebentr-deleted.eentr, tpo.ebsaid=tpo.ebsaid-deleted.esaid, tpo.bentr=tpo.bentr-deleted.entr, tpo.bsaid=tpo.bsaid-deleted.said from deleted where tpo.tpostamp=deleted.tpostamp UPDATE TPO SET tpo.ebentr=tpo.ebentr+inserted.eentr, tpo.ebsaid=tpo.ebsaid+inserted.esaid, tpo.bentr=tpo.bentr+inserted.entr, tpo.bsaid=tpo.bsaid+inserted.said, TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where tpo.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oz_update ON OZ WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE OL SET ol.olstamp=inserted.ozstamp,ol.ozstamp=inserted.ozstamp,ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='OZ',ol.documento=inserted.docnome,ol.cheque=inserted.cheque,ol.data=inserted.data,ol.dvalor=inserted.dvalor,ol.entr=inserted.entr,ol.eentr=inserted.eentr,ol.said=inserted.said,ol.esaid=inserted.esaid,ol.entrm=inserted.entrm2,ol.saidm=inserted.saidm2,ol.moeda=inserted.moeda2,ol.descricao=inserted.descricao,ol.olcodigo=inserted.olcodigo,ol.grupo=inserted.grupo,ol.sgrupo=inserted.sgrupo,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.local,ol.operext=inserted.operext,ol.cecope=inserted.cecope from inserted where ol.ozstamp=inserted.ozstamp  GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fmlc_update ON FMLC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (descricao) BEGIN if update(descricao) BEGINupdate fm set fm.deslocal=inserted.descricao from inserted where fm.fmlcstamp=inserted.fmlcstamp update fmex set fmex.deslocal=inserted.descricao from inserted where fmex.fmlcstamp=inserted.fmlcstamp END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cevi_insert ON cevi WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON		UPDATE CEV
		SET	CEV.npres = CEV.npres + 1,
			CEV.cont1ct = CEV.cont1ct + (CASE WHEN CEV.cont1chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont1chkval)
											  ELSE
											  	   INSERTED.cont1ct
										 END
									    ),
			CEV.cont2ct = CEV.cont2ct + (CASE WHEN CEV.cont2chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont2chkval)
											  ELSE
											  	   INSERTED.cont2ct
										 END
									    ),			
			CEV.cont3ct = CEV.cont3ct + (CASE WHEN CEV.cont3chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont3chkval)
											  ELSE
											  	   INSERTED.cont3ct
										 END
									    ),			
			CEV.cont4ct = CEV.cont4ct + (CASE WHEN CEV.cont4chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont4chkval)
											  ELSE
											  	   INSERTED.cont4ct
									     END
									    ),			
			CEV.cont5ct = CEV.cont5ct + (CASE WHEN CEV.cont5chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont5chkval)
											  ELSE
											  	   INSERTED.cont5ct
									     END
									    ),			
			CEV.cont6ct = CEV.cont6ct + (CASE WHEN CEV.cont6chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont6chkval)
											  ELSE
											  	   INSERTED.cont6ct
									     END
									    ),			
			CEV.cont7ct = CEV.cont7ct + (CASE WHEN CEV.cont7chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont7chkval)
											  ELSE
											  	   INSERTED.cont7ct
									     END
									    ),			
			CEV.cont8ct = CEV.cont8ct + (CASE WHEN CEV.cont8chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont8chkval)
											  ELSE
											  	   INSERTED.cont8ct
									     END
									    ),			
			CEV.cont9ct = CEV.cont9ct + (CASE WHEN CEV.cont9chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont9chkval)
											  ELSE
											  	   INSERTED.cont9ct
									     END
									    )			
		FROM INSERTED
		WHERE CEV.cevstamp = INSERTED.cevstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cevi_delete ON cevi WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON		UPDATE CEV
		SET CEV.npres = CEV.npres - 1
		FROM DELETED
		WHERE CEV.cevstamp = DELETED.cevstamp AND CEV.npres > 0
		-- Trata a remoção da inscrição
		UPDATE CEV
		SET	CEV.cont1ct = CEV.cont1ct - (CASE WHEN CEV.cont1chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont1chkval)
											  ELSE
											  	   DELETED.cont1ct
									     END
									    ),
			CEV.cont2ct = CEV.cont2ct - (CASE WHEN CEV.cont2chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont2chkval)
											  ELSE
											  	   DELETED.cont2ct
									     END
									    ),			
			CEV.cont3ct = CEV.cont3ct - (CASE WHEN CEV.cont3chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont3chkval)
											  ELSE
											  	   DELETED.cont3ct
									     END
									    ),			
			CEV.cont4ct = CEV.cont4ct - (CASE WHEN CEV.cont4chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont4chkval)
											  ELSE
											  	   DELETED.cont4ct
									     END
									    ),			
			CEV.cont5ct = CEV.cont5ct - (CASE WHEN CEV.cont5chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont5chkval)
											  ELSE
											  	   DELETED.cont5ct
									     END
									    ),			
			CEV.cont6ct = CEV.cont6ct - (CASE WHEN CEV.cont6chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont6chkval)
											  ELSE
											  	   DELETED.cont6ct
									     END
									    ),			
			CEV.cont7ct = CEV.cont7ct - (CASE WHEN CEV.cont7chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont7chkval)
											  ELSE
											  	   DELETED.cont7ct
									     END
									    ),			
			CEV.cont8ct = CEV.cont8ct - (CASE WHEN CEV.cont8chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont8chkval)
											  ELSE
											  	   DELETED.cont8ct
									     END
									    ),			
			CEV.cont9ct = CEV.cont9ct - (CASE WHEN CEV.cont9chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont9chkval)
											  ELSE
											  	   DELETED.cont9ct
									     END
									    )
		FROM DELETED			
		WHERE CEV.cevstamp = DELETED.cevstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fcc_update ON FCC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(idfcc) and (select count(*) from clb inner join deleted on clb.fccstamp=deleted.fccstamp)>0 begin update clb set clb.nmfcc=inserted.idfcc from inserted inner join deleted on inserted.fccstamp=deleted.fccstamp where clb.nmfcc=deleted.idfcc endif update(idfcc) and (select count(*) from rct inner join deleted on rct.fccstamp=deleted.fccstamp)>0 begin update rct set rct.nmfcc=inserted.idfcc from inserted inner join deleted on inserted.fccstamp=deleted.fccstamp where rct.nmfcc=deleted.idfcc end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_em_update ON EM WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome)
begin
update cts set
cts.nomeem = i.nome
from inserted as i inner join cts on cts.noem=i.no
where cts.noem=i.no
update vi set
vi.nome=i.nome
from inserted as i inner join vi on vi.no=i.no
where vi.no=i.no
update ng set
ng.contacto=i.nome
from inserted as i inner join ng on ng.nocontacto=i.no
where ng.nocontacto=i.no
end
--&&57973
IF update(codig1)
UPDATE vi SET 
vi.codig1 = i.codig1
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.codig1 = d.codig1
IF update(codig2)
UPDATE vi SET 
vi.codig2 = i.codig2
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.codig2 = d.codig2
IF update(codig3)
UPDATE vi SET 
vi.codig3 = i.codig3
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.codig3 = d.codig3
IF update(ctipo)
UPDATE vi SET 
vi.ctipo = i.ctipo
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.ctipo = d.ctipo
IF update(telefone)
UPDATE vi SET 
vi.telefone = i.telefone
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.telefone = d.telefone
IF update(morada)
UPDATE vi SET 
vi.morada = i.morada
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.morada = d.morada
IF update(codpost)
UPDATE vi SET 
vi.codpost = i.codpost
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.codpost = d.codpost
IF update(local)
UPDATE vi SET 
vi.local = i.local
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.local = d.local
IF update(vendedor) OR update(vendnm)				
UPDATE vi SET 
vi.vendedor = i.vendedor, vi.vendnm = i.vendnm
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.vendedor = d.vendedor
IF update(zona)				
UPDATE vi SET 
vi.zona = i.zona
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.zona = d.zona
IF update(fref)
UPDATE vi SET 
vi.fref = i.fref
from inserted as i 
inner join deleted as d on d.emstamp = i.emstamp
inner join vi on vi.no = i.no
where vi.no = i.no and vi.lsitu = 0 and vi.fref = d.fref
--&&57973
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fp_delete ON FP WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUpdate abs set abs.classif = 0 from deleted where abs.classif = 1 and abs.absstamp=deleted.absstamp and deleted.absstamp not in (select fp.absstamp from fp where fp.no = deleted.no and fp.fpstamp <> deleted.fpstamp)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lg_delete ON lg WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM FC from deleted where fc.lgstamp=deleted.lgstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_formb_update ON formb WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF update(ecran) Begin Update Formc set Formc.ecran=inserted.ecran from inserted inner join formc on inserted.formbstamp=formc.idustamp End IF update(dfdidoc) Begin Update Formc set Formc.dfdidoc=inserted.dfdidoc from inserted inner join formc on inserted.formbstamp=formc.idustamp End GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ml_update ON ml WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update(dino) or update(conta) or update(deb) or update(cre) or update(edeb) or update(ecre) or update(data) or update(mes) or update(debl) or update(recapval) or update(erecapval) BEGIN  declare @d as datetime, @dx as datetime, @mesv int  select @d = inserted.data from inserted   select @mesv=isnull((Select convert(int,valor) from para1 where descricao='ml_mes1'),0)  select @dx=DATEADD(month,@mesv-1 ,@d) declare @fvar int select @fvar = 15 while @fvar>=1BEGINUPDATE PCSA SET pcsa.deb=pcsa.deb-isnull((select sum(deleted.deb) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0),pcsa.cre=pcsa.cre-isnull((select sum(deleted.cre) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0), pcsa.edeb=pcsa.edeb-isnull((select sum(deleted.edeb) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0),pcsa.ecre=pcsa.ecre-isnull((select sum(deleted.ecre) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0) from deleted where pcsa.conta=substring(deleted.conta,1,@fvar) and pcsa.ano=year(deleted.data) and pcsa.mes=deleted.mes and @fvar<=len(rtrim(deleted.conta)) select @fvar = @fvar -1  ENDdeclare @ivar int select @ivar = 15 while @ivar>.5BEGININSERT INTO PCSA (pcsastamp,conta,ano,mes)select distinct substring(inserted.conta,1,@ivar)+convert(char(4),year(inserted.data))+convert(char(2),inserted.mes),substring(inserted.conta,1,@ivar),year(inserted.data), inserted.mesfrom inserted where @ivar<=.5+len(rtrim(inserted.conta)) AND not EXISTS(select conta from pcsa where pcsa.conta=substring(inserted.conta,1,@ivar) and pcsa.ano=year(inserted.data) and pcsa.mes=inserted.mes and @ivar<=.5+len(rtrim(inserted.conta))) and EXISTS(select conta from pc (nolock) where pc.ano = YEAR(inserted.data) and pc.conta = substring(inserted.conta,1,@ivar)) UPDATE PCSA SET pcsa.deb=pcsa.deb+isnull((select sum(inserted.deb) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0),pcsa.cre=pcsa.cre+isnull((select sum(inserted.cre) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0), pcsa.edeb=pcsa.edeb+isnull((select sum(inserted.edeb) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0),pcsa.ecre=pcsa.ecre+isnull((select sum(inserted.ecre) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0) from INSERTED where pcsa.conta=substring(inserted.conta,1,@ivar) and pcsa.ano=year(inserted.data) and pcsa.mes=inserted.mes and @ivar<=len(rtrim(inserted.conta)) select @ivar = @ivar -1  END END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ox_delete ON ox WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.oxstamp=deleted.oxstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tn_atendido ON TN WITH ENCRYPTION FOR INSERT,UPDATE  AS SET NOCOUNT ON IF update (iatendido) BEGIN update tn set tn.atendido=inserted.atendido,tn.ahora=inserted.ahora,tn.adata=inserted.adata,tn.duracao=inserted.duracao,tn.aquem=inserted.aquem from inserted where tn.oristamp=inserted.oristamp  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sv_insert ON sv WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON IF update (ref) or update (tt) or update (cm) or update (lote) or update (data)  BEGIN DECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTDECLARE @@STICLASTDATE datetimeSELECT @@STICLASTDATE = (select case when exists(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc)  then convert(datetime,(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc),121) else convert(datetime,'19000101',121) end) IF @@MROW = 1 BEGIN IF exists(select * from inserted where inserted.data < @@STICLASTDATE)RETURN END INSERT INTO SA (sastamp,armazem,ref) select convert(char(5),armazem)+ref,armazem,ref from inserted where not EXISTS(select * from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and inserted.data >= @@STICLASTDATE UPDATE ST SET st.pcpond=case when st.stock>0  then case when inserted.cm<50 then (st.pcpond*st.stock+inserted.tt)/(st.stock) else (st.pcpond*st.stock-inserted.tt)/(st.stock) end else st.pcpond end,st.epcpond=case when st.stock>0  then case when inserted.cm<50 then (st.epcpond*st.stock+inserted.ett)/(st.stock) else (st.epcpond*st.stock-inserted.ett)/(st.stock) end else st.epcpond end  from INSERTED  inner join cm2 on cm2.cm = inserted.cm where (cm2.MUDAPCPOND = 1 or cm2.MUDAPCULT=1) and st.ref=inserted.ref   and inserted.data >= @@STICLASTDATE  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ts_update ON TS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nmdos) BEGIN  update WKDOCS set wkdocs.wkldocnome = inserted.nmdos  from inserted, wkdocs, deleted  where wkdocs.wkldocnome=deleted.nmdos and wkdocs.wkldocno= deleted.ndos  update WK set wk.wkdocdescr = inserted.nmdos  from inserted, wk, deleted  where wk.wkdocdescr=deleted.nmdos and wk.wkdoc= deleted.ndos END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_camvi_delete ON CAMVI WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON			DELETE FROM camven FROM camven inner join deleted on camven.camvistamp=deleted.camvistamp
			DELETE FROM camvenlist FROM camvenlist inner join deleted on camvenlist.camvistamp=deleted.camvistamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cve_update ON CVE WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(nome) and (select count(*) from srt inner join inserted on srt.cvestamp=inserted.cvestamp)>0 begin update srt set srt.nome=inserted.nome from inserted where srt.cvestamp=inserted.cvestamp endif update(nome) and (select count(*) from etr inner join inserted on etr.cvestamp=inserted.cvestamp)>0 begin update etr set etr.nome=inserted.nome from inserted where etr.cvestamp=inserted.cvestamp end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ml_insert ON ml WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONdeclare @ivar int, @d as datetime, @dx as datetime, @mesv int select @ivar = 15 select @d = inserted.data from inserted select @mesv=isnull((Select convert(int,valor) from para1 where descricao='ml_mes1'),0) select @dx=DATEADD(month,@mesv-1 ,@d) while @ivar>=1BEGININSERT INTO PCSA (pcsastamp,conta,ano,mes)select distinct substring(inserted.conta,1,@ivar)+convert(char(4),year(inserted.data))+convert(char(2),inserted.mes),substring(inserted.conta,1,@ivar),year(inserted.data), inserted.mesfrom inserted inner join pc on pc.conta=substring(inserted.conta,1,@ivar) where @ivar<=.5+len(rtrim(inserted.conta)) AND not EXISTS(select conta from pcsa where pcsa.conta=substring(inserted.conta,1,@ivar) and pcsa.ano=year(inserted.data) and pcsa.mes=inserted.mes and @ivar<=.5+len(rtrim(inserted.conta))) and EXISTS(select conta from pc (nolock) where pc.ano = YEAR(inserted.data) and pc.conta = substring(inserted.conta,1,@ivar)) UPDATE PCSA SET pcsa.deb=pcsa.deb+isnull((select sum(inserted.deb) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0),pcsa.cre=pcsa.cre+isnull((select sum(inserted.cre) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0), pcsa.edeb=pcsa.edeb+isnull((select sum(inserted.edeb) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0),pcsa.ecre=pcsa.ecre+isnull((select sum(inserted.ecre) from inserted where substring(inserted.conta,1,@ivar)=pcsa.conta and year(inserted.data)=pcsa.ano and inserted.mes=pcsa.mes),0) from INSERTED where pcsa.conta=substring(inserted.conta,1,@ivar) and pcsa.ano=year(inserted.data) and pcsa.mes=inserted.mes and @ivar<=len(rtrim(inserted.conta)) select @ivar = @ivar -1  ENDdeclare @oristamp varchar(25);
declare @tabela sysname;
declare @nomecampostamp sysname;
declare @sqlstr nvarchar(2000);

declare c_inserted cursor for
    select distinct 
        oristamp, 
        rtrim(ltrim(origem)) as tabela, 
        rtrim(ltrim(origem)) + 'stamp' as nomecampostamp 
    from inserted
    inner join do on do.dostamp = inserted.dostamp
    where origem is not null
      and origem <> 'DO' 
      and origem <> 'ECAJ' 
      and origem <> 'ECIC' 
      and origem <> 'PR' 
      and origem <> '';

open c_inserted;
fetch next from c_inserted into @oristamp, @tabela, @nomecampostamp;

while @@fetch_status = 0
begin
    set @sqlstr = 
        'update a
         set marcadoenviar = 1
         from anexos a
         inner join ' + QUOTENAME(@tabela) + ' t
            on a.recstamp = t.' + QUOTENAME(@nomecampostamp) + '
         where t.' + QUOTENAME(@nomecampostamp) + ' = @p_oristamp
           and a.oritable = @p_tabela
           and exists (
               select 1 
               from anexos a2 
               where a2.oritable = @p_tabela
                 and a2.recstamp = t.' + QUOTENAME(@nomecampostamp) + '
           )';

    exec sp_executesql @sqlstr, 
        N'@p_oristamp varchar(25), @p_tabela varchar(20)',
        @p_oristamp = @oristamp,
        @p_tabela = @tabela;
		
    fetch next from c_inserted into @oristamp, @tabela, @nomecampostamp;
end

close c_inserted;
deallocate c_inserted;
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rech_update ON RECH WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(usrhora) or update(chdata) or update(clbanco) or update(clcheque) or 									update(echvalor) or update(chvalor) or update(chvalorm)INSERT INTO CH (chstamp,restamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,clbanco,clcheque, fref, contado, ollocal,tptit) select inserted.rechstamp,inserted.restamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.chdata,inserted.ousrdata,inserted.ousrdata,re.no,re.nome,re.estab,'RE',inserted.chvalor,inserted.echvalor,inserted.chvalorm,re.chmoeda,inserted.clbanco,inserted.clcheque,re.fref, re.contado, re.ollocal, inserted.tptit from inserted  inner join re on re.restamp = inserted.restamp where re.cheque=1 and inserted.rechstamp not in (select ch.chstamp from ch where ch.chstamp=inserted.rechstamp) UPDATE CH SET ch.chstamp=inserted.rechstamp,ch.restamp=inserted.restamp,ch.tptit=inserted.tptit,ch.usrdata=inserted.usrdata,ch.usrhora=inserted.usrhora,ch.usrinis=inserted.usrinis,ch.data=inserted.chdata,ch.no=re.no,ch.nome=re.nome,ch.estab=re.estab,ch.origem='RE',ch.valor=inserted.chvalor,ch.evalor=inserted.echvalor,ch.valorm=inserted.chvalorm,ch.moeda=re.chmoeda,ch.clbanco=inserted.clbanco,ch.clcheque=inserted.clcheque,ch.fref=re.fref,ch.contado=re.contado,ch.ollocal=re.ollocal from inserted inner join re on re.restamp = inserted.restamp where re.cheque=1 and inserted.rechstamp=ch.chstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_us_insert ON US WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON 
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.tpe') AND type in (N'U'))
if (select SUM(case when RTRIM(inserted.ugstamp) = '' or  inserted.ugstamp is null then 0 else 1 end) from inserted) > 0 
BEGIN 
INSERT INTO TME(tmestamp,tred,trestamp,pfstamp,usno,usna,eadmin,nsegue,ousrinis,ousrdata, ousrhora,usrinis,usrdata,usrhora,marcada,prefe,nread) 
select substring(cast(newid() as varchar(40)),0,25) as 'tmestamp', * from (
select distinct tpe.tred, tpe.trestamp, tpe.pfstamp, inserted.userno, inserted.username, '0' as 'eadmin', '0' as 'nsegue', inserted.ousrinis, inserted.ousrdata, inserted.ousrhora,  
inserted.usrinis, inserted.usrdata, inserted.usrhora, '0' as 'marcada', '0' as 'prefe', '0' as 'nread' from inserted
inner join pfg on inserted.ugstamp = pfg.ugstamp 
inner join tpe on pfg.pfstamp = tpe.pfstamp 
where inserted.usstamp not in(select us.usstamp from tme inner join us on tme.usno = us.userno where tme.trestamp = tpe.trestamp) ) us 
END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_us_delete ON US WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON IF (select count(*) from deleted) > 0
 BEGIN delete from ushist from deleted where ushist.userno=deleted.userno END
 BEGIN delete from umenu from deleted where umenu.userno=deleted.userno END 
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.tpe') AND type in (N'U'))
begin
IF(select count(*) from pfu,deleted,pfg where pfu.usstamp=deleted.usstamp or pfg.ugstamp=deleted.ugstamp) > 0 
BEGIN 
delete from tme FROM deleted
where (RTRIM(tme.trestamp) + RTRIM(cast(tme.usno as varchar(10))) in ( 
select distinct RTRIM(tpe.trestamp) + RTRIM(cast(deleted.userno as varchar(10))) from deleted
left join pfg on deleted.ugstamp = pfg.ugstamp 
left join pfu on deleted.usstamp = pfu.usstamp 
inner join tpe on pfg.pfstamp=tpe.pfstamp or pfu.pfstamp=tpe.pfstamp 
inner join tme on tme.usno = deleted.userno and tme.trestamp=tpe.trestamp ))
DELETE FROM PFU FROM deleted 
where pfu.usstamp=deleted.usstamp 					
end 
end	GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fo2_insert ON fo2 WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select COUNT(*) from inserted where inserted.formapag='')>0
UPDATE fo2 SET formapag=1 
from inserted
WHERE fo2.fo2stamp=inserted.fo2stamp AND fo2.formapag=''			GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tfo_insert ON tfo WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tfo_update ON tfo WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sftnome_delete ON sftnome WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM SFTAM FROM deleted where sftam.nome=deleted.campoGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_olbb_delete ON olbb WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM OL FROM deleted where ol.olbbstamp=deleted.olbbstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oy_delete ON oy WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE CH SET ch.jadepo=0,ch.dataoy='' from deleted where ch.chstamp=deleted.chstamp  UPDATE ch  SET ch.status =  (select top 1 cm  from ( select chl.cm, chl.ousrdata, chl.ousrhora     from chl INNER JOIN ch ON ch.chstamp = chl.chstamp    inner join deleted on deleted.chstamp = ch.chstamp    union all  select ox.cm, oy.ousrdata, oy.ousrhora  from ox inner join oy on ox.oxstamp = oy.oxstamp  inner join deleted on deleted.chstamp = oy.chstamp	  )t  order by ousrdata+ousrhora desc)   from deleted, ch where deleted.chstamp = ch.chstamp  GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ow_update ON OW WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF (select count(*) from inserted where inserted.ozstamp<>'' or inserted.postamp<>'' or inserted.restamp<>'' or (inserted.ollinhas = 1 and inserted.owliolcod = 1))>0 DELETE FROM OL from inserted where ol.owstamp=inserted.owstamp and ol.owlistamp = '' and (inserted.ozstamp<>'' or inserted.postamp<>'' or inserted.restamp<>'' or (inserted.ollinhas = 1 and inserted.owliolcod = 1))IF (select count(*) from inserted where inserted.ozstamp='' and inserted.postamp='' and inserted.restamp='' and ((inserted.ollinhas=1 and inserted.owliolcod=0) or inserted.ollinhas = 0))>0 UPDATE OL SET ol.olstamp=inserted.owstamp,ol.owstamp=inserted.owstamp,ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='OW',ol.documento=inserted.docnome,ol.cheque=inserted.cheque,ol.data=inserted.data,ol.dvalor=inserted.dvalor,ol.entr=inserted.entr,ol.eentr=inserted.eentr,ol.said=inserted.said,ol.esaid=inserted.esaid,ol.entrm=inserted.entrm2,ol.saidm=inserted.saidm2,ol.moeda=inserted.moeda2,ol.descricao=inserted.descricao,ol.olcodigo=inserted.olcodigo,ol.grupo=inserted.grupo,ol.sgrupo=inserted.sgrupo,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.local,ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp,ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=inserted.operext from inserted where ol.owstamp=inserted.owstamp and ol.owlistamp = ''IF (update(docnome) or update(cheque) or update(data) or update(dvalor) or update(fref) or update(intid) or update(ncusto) or update(ollocal) or update(contado) or update(local) or update(site) or update(pnome) or update(pno) or update(cxstamp) or update(ssstamp) or update(ssusername)) and (select count(*) from inserted where inserted.ollinhas = 1 and inserted.owliolcod = 1)>0 update owli set owli.owstamp = inserted.owstamp from inserted where owli.owstamp = inserted.owstamp IF (select count(*) from inserted inner join ol on ol.owstamp = inserted.owstamp where inserted.ozstamp='' and inserted.postamp='' and inserted.restamp='' and ((inserted.ollinhas=1 and inserted.owliolcod=0) or inserted.ollinhas = 0)) = 0INSERT INTO OL (olstamp,owstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,entrm,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext) select inserted.owstamp,inserted.owstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'OW',inserted.docnome,inserted.cheque,inserted.data,inserted.dvalor,inserted.entr,inserted.eentr,inserted.said,inserted.esaid,inserted.entrm2,inserted.saidm2,inserted.moeda2,inserted.descricao,inserted.olcodigo,inserted.grupo,inserted.sgrupo,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.local,inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername,inserted.ssstamp,inserted.ssusername,inserted.operext from inserted where inserted.ozstamp='' and inserted.postamp='' and inserted.restamp='' and ((inserted.ollinhas=1 and inserted.owliolcod=0) or inserted.ollinhas = 0)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_oc_update ON oc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE (olgrupo)BEGINupdate od set od.grupo = inserted.olgrupo from inserted  where od.olcodigo=inserted.olcodigo update ol set ol.grupo = inserted.olgrupo from inserted  where ol.olcodigo=inserted.olcodigo update op set op.grupo = inserted.olgrupo from inserted  where op.olcodigo=inserted.olcodigo update cm1 set cm1.clgrupo = inserted.olgrupo from inserted  where cm1.olcl=inserted.olcodigo update cm1 set cm1.flgrupo = inserted.olgrupo from inserted  where cm1.olfl=inserted.olcodigo update dc set dc.grupo = inserted.olgrupo from inserted  where dc.olcodigo=inserted.olcodigo ENDIF UPDATE (olsgrupo)BEGINupdate od set od.sgrupo = inserted.olsgrupo from inserted  where od.olcodigo=inserted.olcodigo update ol set ol.sgrupo = inserted.olsgrupo from inserted  where ol.olcodigo=inserted.olcodigo update op set op.sgrupo = inserted.olsgrupo from inserted  where op.olcodigo=inserted.olcodigo update cm1 set cm1.clsgrupo = inserted.olsgrupo from inserted  where cm1.olcl=inserted.olcodigo update cm1 set cm1.flsgrupo = inserted.olsgrupo from inserted  where cm1.olfl=inserted.olcodigo update dc set dc.sgrupo = inserted.olsgrupo from inserted  where dc.olcodigo=inserted.olcodigo ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fopd_update ON fopd WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE FC SET fc.difcambio=fc.difcambio-(SELECT SUM(deleted.difcambio) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.edifcambio=fc.edifcambio-(SELECT SUM(deleted.edifcambio) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.debf=fc.debf-(SELECT SUM(deleted.vreg - deleted.virs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.edebf=fc.edebf-(SELECT SUM(deleted.evreg - deleted.evirs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.debfm=fc.debfm-(SELECT SUM(deleted.vmreg) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.virsreg = fc.virsreg - (SELECT SUM(deleted.virs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp), fc.evirsreg = fc.evirsreg - (SELECT SUM(deleted.evirs) from deleted where  deleted.fcstamp <> '' and deleted.fcstamp = fc.fcstamp) from fc inner join deleted on deleted.fcstamp = fc.fcstamp where deleted.fcstamp <> '' update FC set fc.credf=fc.credf-(SELECT SUM(deleted.vreg) from deleted where  deleted.fostamp <> '' and deleted.fostamp = fc.fostamp), fc.ecredf=fc.ecredf-(SELECT SUM(deleted.evreg) from deleted where  deleted.fostamp <> '' and deleted.fostamp = fc.fostamp), fc.credfm=fc.credfm-(SELECT SUM(deleted.vmreg) from deleted where  deleted.fostamp <> '' and deleted.fostamp = fc.fostamp) from fc inner join deleted on deleted.fostamp = fc.fostamp inner join pd on deleted.pdstamp = pd.pdstamp where deleted.fostamp <> '' update pd set pd.integrado=(case isnull((select top 1 fopd.fopdstamp from fopd where fopd.pdstamp=deleted.pdstamp),'') when '' then (case ISNULL((select top 1 pl.fcstamp from pl inner join fc on pl.fcstamp=fc.fcstamp where fc.pdstamp=deleted.pdstamp and fc.origem='PD'),'') when '' then 0 else 1 end) else 1 end) from deleted where pd.pdstamp=deleted.pdstamp UPDATE FC SET fc.difcambio=fc.difcambio+(SELECT SUM(inserted.difcambio) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.edifcambio=fc.edifcambio+(SELECT SUM(inserted.edifcambio) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.debf=fc.debf+(SELECT SUM(inserted.vreg - inserted.virs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.edebf=fc.edebf+(SELECT SUM(inserted.evreg - inserted.evirs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.debfm=fc.debfm+(SELECT SUM(inserted.vmreg) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.virsreg = fc.virsreg + (SELECT SUM(inserted.virs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp), fc.evirsreg = fc.evirsreg + (SELECT SUM(inserted.evirs) from inserted where  inserted.fcstamp <> '' and inserted.fcstamp = fc.fcstamp) from fc inner join inserted on inserted.fcstamp = fc.fcstamp where inserted.fcstamp <> '' update FC set fc.credf=fc.credf+(SELECT SUM(inserted.vreg) from inserted where  inserted.fostamp <> '' and inserted.fostamp = fc.fostamp), fc.ecredf=fc.ecredf+(SELECT SUM(inserted.evreg) from inserted where  inserted.fostamp <> '' and inserted.fostamp = fc.fostamp), fc.credfm=fc.credfm+(SELECT SUM(inserted.vmreg) from inserted where  inserted.fostamp <> '' and inserted.fostamp = fc.fostamp) from fc inner join inserted on inserted.fostamp = fc.fostamp inner join pd on inserted.pdstamp = pd.pdstamp where inserted.fostamp <> '' update pd set pd.integrado=1 from inserted where pd.pdstamp=inserted.pdstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftcc_insert ON ftcc WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO CH (chstamp,ftstamp,ftccstamp, usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis, data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda, clbanco,clcheque, contado, ollocal) select inserted.ftccstamp,inserted.ftstamp,inserted.ftccstamp, ft.usrdata,ft.usrhora,ft.usrinis, ft.ousrdata,ft.ousrhora,ft.ousrinis, inserted.dataven,ft.ousrdata,ft.ousrdata,case when td.lancacli=0 then ft.no else ft2.c2no end, case when td.lancacli=0 then ft.nome else ft2.c2nome end, case when td.lancacli=0 then ft.estab else ft2.c2estab end, 'FTCC', inserted.deb,inserted.edeb,inserted.debm,ft.moeda, inserted.clbanco,inserted.clcheque, case when td.lancaol = 1 then td.vdcontado else 0 end, case when td.lancaol = 1 then td.vdollocal else SPACE(50) end  from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=ft.ftstamp inner join td on td.ndoc=ft.ndoc where ft.anulado = 0 and inserted.cheque=1 INSERT INTO CC (ccstamp,ftstamp,ftccstamp,obs,clcheque,clbanco,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,cambiofixo,deb,edeb,debm,debf,edebf,debfm,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,IVATX1,IVATX2,IVATX3,IVATX4,IVATX5,IVATX6,IVATX7,IVATX8,IVATX9,REEXGIVA,intid,formapag,situacao,moeda,nrdoc,dataven,fref,ccusto,ncusto,zona,segmento,tipo,pais,estab,vendedor,vendnm,valch,evalch,evirs,virs,cobrador,rota,tpstamp,tpdesc) select inserted.ftccstamp,inserted.ftstamp,inserted.ftccstamp,inserted.obs,inserted.clcheque,inserted.clbanco,ft.usrdata,ft.usrhora,ft.usrinis,ft.ousrdata,ft.ousrhora,ft.ousrinis,ft.fdata,case when td.lancacli=0 then ft.no else ft2.c2no end,case when td.lancacli=0 then ft.nome else ft2.c2nome end,td.cmmcc,td.cmmccn,'FT',ft.cobranca,ft.cambiofixo,case when td.tipodoc<>3 then inserted.debcc else 0 end,case when td.tipodoc<>3 then inserted.edebcc else 0 end,case when td.tipodoc<>3 then inserted.debmcc else 0 end,case when td.tipodoc<>3 then (inserted.debcc-inserted.deb) else 0 end, case when td.tipodoc<>3 then (inserted.edebcc-inserted.edeb) else 0 end, case when td.tipodoc<>3 then (inserted.debmcc-inserted.debm) else 0 end,inserted.ivav1,inserted.ivav2,inserted.ivav3,inserted.ivav4,inserted.ivav5,inserted.ivav6,inserted.ivav7,inserted.ivav8,inserted.ivav9,inserted.eivav1,inserted.eivav2,inserted.eivav3,inserted.eivav4,inserted.eivav5,inserted.eivav6,inserted.eivav7,inserted.eivav8,inserted.eivav9,ft.IVATX1,ft.IVATX2,ft.IVATX3,ft.IVATX4,ft.IVATX5,ft.IVATX6,ft.IVATX7,ft.IVATX8,ft.IVATX9,ft2.REEXGIVA,ft.intid,inserted.formapag,'1',ft.moeda,ft.fno,inserted.dataven,ft.fref,ft.ccusto,ft.ncusto,ft.zona,ft.segmento,ft.tipo,case when td.lancacli=0 then ft.pais else ft2.c2pais end,case when td.lancacli=0 then ft.estab else ft2.c2estab end,ft.vendedor,ft.vendnm,case when inserted.cheque=1 and td.tipodoc<>3 then inserted.deb else 0 end,case when inserted.cheque=1 and td.tipodoc<>3 then inserted.edeb else 0 end,inserted.evirs,inserted.virs,ft.cobrador,ft.rota,ft.tpstamp,ft.tpdesc from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=ft.ftstamp inner join td on td.ndoc=ft.ndoc where ft.anulado = 0 and td.lancacc=1 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_li_insert ON li WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE CC SET cc.debf=cc.debf+case when inserted.rec-inserted.arred>0 then inserted.rec-inserted.arred else 0 end from inserted where inserted.ccstamp=cc.ccstampupdate cc set cc.credf=cc.credf-case when inserted.rec-inserted.arred<0 then inserted.rec-inserted.arred else 0 end from inserted where inserted.ccstamp=cc.ccstampupdate cc set cc.edebf=cc.edebf+case when inserted.erec>0 then inserted.erec-inserted.earred else 0 end from inserted where inserted.ccstamp=cc.ccstampupdate cc set cc.ecredf=cc.ecredf-case when inserted.erec<0 then inserted.erec-inserted.earred else 0 end from inserted where inserted.ccstamp=cc.ccstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_owli_update ON OWLI WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF (select count(*) from ow inner join inserted on inserted.owstamp = ow.owstamp where ow.ozstamp<>'' or ow.postamp<>'' or ow.restamp<>'' or ((ow.ollinhas = 1 and ow.owliolcod = 0) or ow.ollinhas = 0))>0 DELETE FROM OL from inserted inner join ow on ow.owstamp = inserted.owstamp where ol.owlistamp=inserted.owlistamp and (ow.ozstamp<>'' or ow.postamp<>'' or ow.restamp<>'' or ((ow.ollinhas = 1 and ow.owliolcod = 0) or ow.ollinhas = 0))IF (select count(*) from inserted inner join ow on ow.owstamp = inserted.owstamp where ow.ozstamp='' and ow.postamp='' and ow.restamp='' and ow.ollinhas=1 and ow.owliolcod=1)>0 UPDATE OL SET ol.olstamp=inserted.owlistamp,ol.owlistamp=inserted.owlistamp,ol.owstamp=inserted.owstamp,ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='OW',ol.documento=ow.docnome,ol.cheque=ow.cheque,ol.data=ow.data,ol.dvalor=ow.dvalor,ol.entr=(case when dcli.deb = 0 then inserted.valor else 0 end),ol.eentr=(case when dcli.deb = 0 then inserted.evalor else 0 end),ol.said=(case when dcli.deb = 1 then inserted.valor else 0 end),ol.esaid=(case when dcli.deb = 1 then inserted.evalor else 0 end),ol.entrm=(case when dcli.deb = 0 then inserted.valorm2 else 0 end),ol.saidm=(case when dcli.deb = 1 then inserted.valorm2 else 0 end),ol.moeda=ow.moeda2,ol.descricao=inserted.oldesc,ol.olcodigo=inserted.olcodigo,ol.grupo=inserted.grupo,ol.sgrupo=inserted.sgrupo,ol.fref=ow.fref,ol.intid=ow.intid,ol.ccusto=inserted.ccusto,ol.ncusto=ow.ncusto,ol.ollocal=ow.ollocal,ol.contado=ow.contado,ol.local=ow.local,ol.site=ow.site,ol.pnome=ow.pnome,ol.pno=ow.pno,ol.cxstamp=ow.cxstamp,ol.ssstamp=ow.ssstamp,ol.ssusername=ow.ssusername,ol.operext=ow.operext,ol.cecope=inserted.cecope from inserted inner join ow on ow.owstamp = inserted.owstamp inner join dcli on dcli.dclistamp = inserted.dclistamp where ol.owlistamp=inserted.owlistamp IF (select count(*) from OL (nolock) inner join inserted on ol.owlistamp = inserted.owlistamp inner join ow (nolock) on ow.owstamp = inserted.owstamp and ow.ozstamp='' and ow.postamp='' and ow.restamp='' where ow.ollinhas=1 and ow.owliolcod=1) = 0 INSERT INTO OL (olstamp,owlistamp,owstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,entrm,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext,cecope) select inserted.owlistamp,inserted.owlistamp,inserted.owstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'OW',ow.docnome,ow.cheque,ow.data,ow.dvalor,(case when dcli.deb = 0 then inserted.valor else 0 end),(case when dcli.deb = 0 then inserted.evalor else 0 end),(case when dcli.deb = 1 then inserted.valor else 0 end),(case when dcli.deb = 1 then inserted.evalor else 0 end),(case when dcli.deb = 0 then inserted.valorm2 else 0 end),(case when dcli.deb = 1 then inserted.valorm2 else 0 end),ow.moeda2,inserted.oldesc,inserted.olcodigo,inserted.grupo,inserted.sgrupo,ow.fref,ow.intid,inserted.ccusto,ow.ncusto,ow.ollocal,ow.contado,ow.local,ow.site,ow.pnome,ow.pno,ow.cxstamp,ow.cxusername,ow.ssstamp,ow.ssusername,ow.operext,inserted.cecope from inserted inner join ow on ow.owstamp = inserted.owstamp and ow.ozstamp='' and ow.postamp='' and ow.restamp='' and ow.ollinhas=1 and ow.owliolcod=1 inner join dcli on dcli.dclistamp = inserted.dclistamp Where inserted.valor <> 0 OR inserted.evalor <> 0GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pl_delete ON pl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONupdate FC
set fc.credf =fc.credf -isnull((select (case when sum(deleted.cambio)=0 and sum(deleted.escrec)>0 then sum(deleted.escrec)- sum(deleted.arred)
else case when sum(deleted.cambio)<>0 and sum(deleted.escrec)>0 then sum(deleted.escrec) else 0 end end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.debf =fc.debf +isnull((select (case when sum(deleted.cambio)=0 and sum(deleted.escrec)<0 then sum(deleted.escrec)- sum(deleted.arred)
else case when sum(deleted.cambio)<>0 and sum(deleted.escrec)<0 then sum(deleted.escrec) else 0 end end)
from deleted where
deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.credfm =fc.credfm -isnull((select (case when sum(deleted.ecambio)<>0 and sum(deleted.rec)>0 then sum(deleted.rec) else 0 end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.debfm =fc.debfm +isnull((select (case when sum(deleted.ecambio)<>0 and sum(deleted.rec)<0 then sum(deleted.rec) else 0 end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.ecredf =fc.ecredf -isnull((select (case when sum(deleted.erec)>0 then sum(deleted.erec)-sum(deleted.earred) else 0 end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.edebf =fc.edebf +isnull((select (case when sum(deleted.erec)<0 then sum(deleted.erec)-sum(deleted.earred) else 0 end) from deleted where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.virsreg =fc.virsreg -isnull((select sum(deleted.virs) from deleted where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.evirsreg =fc.evirsreg -isnull((select sum(deleted.evirs) from deleted where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
from deleted
where deleted.fcstamp =fc.fcstamp and deleted.process=1

update fc
set fc.evalpo =fc.evalpo - (select sum(deleted.erec -deleted.eivacativado) from deleted where deleted.fcstamp =fc.fcstamp)
, fc.valpo =fc.valpo - (select sum(deleted.escrec -deleted.ivacativado) from deleted where deleted.fcstamp =fc.fcstamp)
, fc.mvalpo =fc.mvalpo - (select sum(deleted.rec -deleted.mivacativado) from deleted where deleted.fcstamp =fc.fcstamp)
, fc.ultdoc =''
, fc.recibado =0
from deleted
where fc.fcstamp =deleted.fcstamp

update FC
set fc.irsdif =fc.irsdif -(select sum(case when deleted.desconto=100 or po.fin=100 then 0
else (round((deleted.virs/(1-(case when deleted.desconto<>0 then deleted.desconto else po.fin end/100))),0)-deleted.virs) end)
from deleted
inner join po on po.postamp =deleted.postamp
where deleted.fcstamp =fc.fcstamp)
, fc.eirsdif =fc.eirsdif -(select sum(case when deleted.desconto=100 or po.fin=100 then 0
else (round((deleted.evirs/(1-(case when deleted.desconto<>0 then deleted.desconto else po.fin end/100))),2)-deleted.evirs) end)
from deleted
inner join po on po.postamp=deleted.postamp
where deleted.fcstamp=fc.fcstamp)
from deleted
inner join po on po.postamp=deleted.postamp
where fc.fcstamp =deleted.fcstamp
and deleted.process=1 and (deleted.desconto<>0 or po.fin<>0)

if ( select count(*) from deleted where deleted.fcorigem='PD')>0
update pd 
set pd.integrado = (case isnull((select top 1 pl.fcstamp 
from pl 
inner join fc on fc.fcstamp =pl.fcstamp 
where fc.fcstamp =deleted.fcstamp and fc.origem='PD'),'') 
when '' 
then (case isnull((select top 1 fopd.fopdstamp 
from fopd 
inner join fc on fc.fcstamp=fopd.fcstamp 
where fc.fcstamp=deleted.fcstamp and fc.origem='PD'),'') 
when '' then 0 else 1 end)
else 1 end)
from deleted 
inner join fc on fc.fcstamp=deleted.fcstamp 
inner join pd on pd.pdstamp=fc.pdstamp 
where fc.fcstamp =deleted.fcstamp 
and deleted.fcorigem='PD'GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sgc_delete ON SGC WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete sx from sx inner join deleted on sx.ref=deleted.ref where sx.cor=deleted.corGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tta_insert ON tta WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE TFA SET TFA.hreal=TFA.hreal+inserted.hreal,TFA.hprev=TFA.hprev+inserted.hprev from INSERTED where TFA.tfastamp=inserted.tfastamp UPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_tta_update ON tta WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE TFA SET TFA.hreal=TFA.hreal-deleted.hreal,TFA.hprev=TFA.hprev-deleted.hprev from DELETED where TFA.tfastamp=deleted.tfastamp UPDATE TFA SET TFA.hreal=TFA.hreal+inserted.hreal,TFA.hprev=TFA.hprev+inserted.hprev from INSERTED where TFA.tfastamp=inserted.tfastamp UPDATE TPO SET TPO.dultima=(case when tpo.dultima>inserted.usrdata then tpo.dultima else inserted.usrdata end) from INSERTED where TPO.tpostamp=inserted.tpostamp 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_e1ca_delete ON e1ca WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDELETE FROM E1LI FROM deleted where e1li.e1castamp=deleted.e1castampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER ON
GO

create FUNCTION fn_iBERIA_Calc_RL(@ccstamp as CHAR(25), @data_fim as datetime)
RETURNS NUMERIC(20,6)
with encryption
AS
BEGIN
	DECLARE @mySaldo NUMERIC(20,6)

	set @mySaldo = 	(			
			select
				sum(rl.erec)
			from rl (nolock)
				join re (nolock) on	re.restamp=rl.restamp
							and re.process=1
							and re.procdata<=@data_fim
							and rl.ccstamp=@ccstamp
			)

	RETURN ISNULL(@mySaldo,0)
end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftreg_insert ON ftreg WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE CC set cc.debf=(case when cc.debf+round(inserted.vciva,0)>=cc.deb then cc.deb else cc.debf+round(inserted.vciva,0) end), cc.edebf=(case when cc.edebf+round(inserted.evciva,2)>=cc.edeb then cc.edeb else cc.edebf+round(inserted.evciva,2) end), cc.debfm=(case when ROUND(cc.debfm+inserted.vmoeda,2)>=cc.debm then cc.debm else cc.debfm+inserted.vmoeda end), cc.ultdoc=inserted.ultdoc from inserted where cc.ftstamp=inserted.ftregstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_vz_update ON vz WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE FROM OL FROM inserted where inserted.process=0 and inserted.vzstamp=ol.vzstamp UPDATE OL SET ol.olstamp=inserted.vzstamp,ol.vzstamp=inserted.vzstamp,ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='VZ',ol.documento=inserted.documento,ol.cheque=inserted.adoc,ol.data=inserted.procdata,ol.dvalor=inserted.dvalor,ol.said=inserted.total,ol.esaid=inserted.etotal,ol.descricao='Pagamento de comissões do Vendedor: '+rtrim(inserted.nome)+', nº '+convert(char,inserted.no),ol.olcodigo=inserted.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo,ol.fref=inserted.fref,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.telocal,ol.operext=inserted.operext,ol.cecope=inserted.cecope from inserted inner join oc on oc.olcodigo=inserted.olcodigo where ol.vzstamp=inserted.vzstamp and inserted.process=1INSERT INTO OL (olstamp,vzstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,operext,cecope) select inserted.vzstamp,inserted.vzstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'VZ',inserted.documento,inserted.adoc,inserted.procdata,inserted.dvalor,inserted.total,inserted.etotal,'Pagamento de comissões do Vendedor: '+rtrim(inserted.nome)+', nº '+convert(char,inserted.no),inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.telocal,inserted.operext,inserted.cecope from inserted inner join oc on oc.olcodigo=inserted.olcodigo where inserted.process=1 and inserted.vzstamp not in (select ol.vzstamp from ol where ol.vzstamp=inserted.vzstamp) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_td_delete ON TD WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdelete from td2 from td2 inner join deleted on td2.td2stamp = deleted.tdstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_hs_update ON HS WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF UPDATE(anulada)UPDATE abs SET abs.anulada = inserted.anulada FROM inserted where inserted.ntipo = 2 and abs.absstamp=inserted.absstamp and inserted.absstamp not in (select hs.absstamp from hs where hs.no = inserted.no and hs.hsstamp <> inserted.hsstamp)
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chf_update ON CHF WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128)
SELECT @Cinfo = Context_Info() 
IF @Cinfo = 0x56587 
RETURN 


IF update(status)
BEGIN 		

Insert into chfl (chflstamp, data, cm, chdesc, lordem
, chfstamp, ousrinis, ousrdata
, ousrhora, usrinis, usrdata, usrhora, obs) 
(select  LEFT(CAST(newID() as varchar(50)),25),inserted.datastatus,status
, CASE status
WHEN 1 then 'Emissão'
WHEN 2 then 'Liquidação' 
WHEN 3 then 'Anulação'					
WHEN 4 then 'Substituição'
END 
, ISNULL((select top 1 chfl.lordem from chfl inner join inserted on inserted.chfstamp = chfl.chfstamp order by chfl.lordem desc),0)+1000
, inserted.chfstamp
, inserted.usrinis, inserted.usrdata, inserted.usrhora
, inserted.usrinis, inserted.usrdata, inserted.usrhora 
, case when status = 4 then ISNULL((select top 1 cheque from chf where chfstamp = inserted.deststamp),'') else '' end 
from inserted
where inserted.status not in (isnull((select top 1 chfl.cm from chfl where inserted.chfstamp = chfl.chfstamp order by chfl.lordem desc),0))
) 

END 

IF exists (select 1 from deleted inner join inserted on inserted.chfstamp = deleted.chfstamp where deleted.status = 2 AND inserted.status <> 2)	
DELETE FROM OL FROM deleted inner join ol on deleted.chfstamp = ol.oristamp WHERE ol.origem = 'CHF' 

IF exists (select 1 from inserted inner join deleted on deleted.chfstamp = inserted.chfstamp where inserted.status = 2 AND deleted.status <> 2)	 
INSERT INTO ol 
(transf,olstamp,oristamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata
,origem,fref,ccusto,ncusto,dvalor,local
,ollocal,contado,data,descricao
,said,esaid,moeda,olcodigo,grupo,sgrupo,cheque,documento)	
SELECT 1,left(CAST(newid() as varchar(50)),25),inserted.chfstamp,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata
,'CHF',inserted.fref,inserted.ccusto,inserted.ncusto,inserted.datastatus,case when inserted.contado>0 then 'B' else 'C' end
,inserted.ollocal,inserted.contado,inserted.datastatus,'Liquidação' 
,inserted.valor,inserted.evalor,'PTE ou EURO','Trf','Transferências','Transferências',inserted.cheque,'Título emitido'
FROM inserted 
WHERE not exists (select 1 from ol where ol.oristamp = inserted.chfstamp and ol.origem = 'CHF')				
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chl_insert ON chl WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from inserted where (inserted.edespesas+inserted.eimpselo)<>0 or (inserted.despesas+inserted.impselo)<>0)>0INSERT INTO OL (olstamp,chlstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,fref,ccusto,ncusto,documento,cheque,data,dvalor,said,esaid,moeda,descricao,olcodigo,grupo,sgrupo,ollocal,contado,local) select inserted.chlstamp,inserted.chlstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'CH',ch.fref,ch.ccusto,ch.ncusto,'Título de tesouraria',convert(char(15),ch.clcheque),inserted.data,inserted.data,(inserted.despesas+inserted.impselo),(inserted.edespesas+inserted.eimpselo),'PTE ou EURO',inserted.chldesc+': '+ch.clcheque,oc.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.ollocal,inserted.contado,inserted.localol from inserted inner join oc on oc.olcodigo='P10001' inner join ch on ch.chstamp = inserted.chstamp where inserted.cm=3  IF (select count(*) from inserted where inserted.cm = 3)> 0  begin  insert into ol (olstamp, data, cheque, documento, descricao, entr,eentr,said,esaid,ollocal,local,sgrupo,grupo,origem,contado,olcodigo,fref,ccusto,ncusto,dvalor,moeda,entrm,saidm,transf,ousrinis,ousrdata,ousrhora,usrinis,usrdata,usrhora,chlstamp)  select substring(cast(newid() as varchar (100)),1,25) as olstamp  , inserted.data, convert(char(15),ch.clcheque) as cheque, 'Título de tesouraria' as documento, inserted.chldesc as descricao, case when ol.entr > 0 then 0 else ch.valor end as entr  , case when ol.eentr > 0 then 0 else ch.evalor end as eentr, case when ol.said > 0 then 0 else ch.valor end as said, case when ol.esaid > 0 then 0 else ch.evalor end as esaid  , ol.ollocal, ol.local, ol.sgrupo, ol.grupo, 'CH' as origem, ol.contado, ol.olcodigo, ol.fref, ol.ccusto, ol.ncusto, ol.dvalor, ol.moeda  , ol.entrm, ol.saidm, ol.transf, inserted.ousrinis as ousrinis, inserted.ousrdata as ousrdata, inserted.ousrhora as ousrhora  , inserted.usrinis as usrinis, inserted.usrdata as usrdata, inserted.usrhora as usrhora, inserted.chlstamp as chlstamp  from ol inner join ox on ol.oxstamp = ox.oxstamp  inner join oy on ox.oxstamp = oy.oxstamp  inner join ch on oy.chstamp = ch.chstamp  inner join chl on chl.chstamp = ch.chstamp  inner join inserted on inserted.chlstamp = chl.chlstamp   where ol.oxstamp in   (  select top 1 ox.oxstamp  from ox inner join oy on ox.oxstamp=oy.oxstamp	inner join inserted on inserted.chstamp = oy.chstamp  order by oy.ousrdata+oy.ousrhora desc  )  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_mt_insert ON MT WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONupdate va set va.kmsatehoje=inserted.kms-va.kmsinicial, va.ultimokm=inserted.kms from inserted where inserted.matricula=va.matricula and inserted.kms>va.ultimokm and inserted.realizada=1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_chl_update ON chl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON if update(plano)  BEGIN   if (select COUNT(*) from inserted where inserted.plano = 0)> 0  BEGIN  update chl  set dinoplano = '', dilnoplano = 0, diaplano = 0, dostamp = '',dplano='19000101'  from chl, inserted where inserted.chlstamp = chl.chlstamp  END  ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lm_update ON lm WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (Select count(*) from inserted where inserted.cm=1 or inserted.cm=19 or inserted.cm=27 or inserted.cm=20 or inserted.cm=9 or inserted.cm=14 or inserted.cm=26 or inserted.cm=10 or inserted.cm=15 or inserted.cm=5)>0 if (Select count(*) from od,inserted where od.lrstamp=inserted.lrstamp and od.origem='LR')=0 INSERT INTO OD (lrstamp,lmstamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,entr,eentr,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,boacobranca) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LR',lr.nomeserie,ltrim(str(inserted.letra,15,0)),lr.vdata,lr.valor-lr.regvalor,lr.evalor-lr.eregvalor,ltrim(rtrim(lr.nome))+'-'+inserted.lmdesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lr.fref,lr.ccusto,lr.ncusto,inserted.ollocal,inserted.contado,inserted.localol ,case when isnull(cl.cobnao,0) = 0 then 1 else 0 end as boacob from inserted inner join lr on lr.lrstamp=inserted.lrstamp left join cl on cl.nome = lr.nome and cl.no = lr.no and cl.estab = lr.estab inner join oc on oc.olcodigo='R00001' where ((lr.valor-lr.regvalor)<>0 or (lr.evalor-lr.eregvalor)<>0) and (inserted.cm=1 or inserted.cm=19 or inserted.cm=27 or inserted.cm=20 or inserted.cm=9 or inserted.cm=14 or inserted.cm=26 or inserted.cm=10 or inserted.cm=15 or inserted.cm=5) and inserted.currec=1 else UPDATE OD SET od.lmstamp=inserted.lmstamp,od.usrdata=inserted.usrdata,od.usrhora=inserted.usrhora,od.usrinis=inserted.usrinis,od.data=lr.vdata,od.entr=(lr.valor-lr.regvalor),od.eentr=(lr.evalor-lr.eregvalor),od.descricao=ltrim(rtrim(lr.nome))+'-'+case when inserted.cm=5 then od.descricao else inserted.lmdesc end,od.fref=lr.fref,od.ccusto=lr.ccusto,od.ncusto=lr.ncusto,od.ollocal=inserted.ollocal,od.contado=inserted.contado,od.local=inserted.localol ,od.boacobranca = case when isnull(cl.cobnao,0) = 0 then 1 else 0 end from inserted inner join lr on lr.lrstamp=inserted.lrstampleft join cl on cl.nome = lr.nome and cl.no = lr.no and cl.estab = lr.estab where od.lrstamp=inserted.lrstamp and od.origem='LR' and (inserted.cm=1 or inserted.cm=19 or inserted.cm=27 or inserted.cm=20 or inserted.cm=9 or inserted.cm=14 or inserted.cm=26 or inserted.cm=10 or inserted.cm=15 or inserted.cm=5) and inserted.currec=1 if (Select count(*) from inserted where inserted.cm=21)>0 if (Select count(*) from cc,inserted where cc.lmstamp=inserted.lmstamp and inserted.cm=21)=0 INSERT INTO CC (lrstamp,ccstamp,lmstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,dataven,no,estab,nome,cm,cmdesc,origem,deb,edeb,nrdoc,fref,intid,ccusto,ncusto,pais,obs,zona) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,lr.sdata,lr.ccdata,lr.no,lr.estab,lr.nome,30,(select cmdesc from cm1 (nolock) where cm=30),'LR',inserted.valor,inserted.evalor,lr.letra,lr.fref,lr.intid,lr.ccusto,lr.ncusto,lr.pais,'Anulação do título de dívida nº '+ltrim(str(inserted.letra,10,0)),cl.zona from inserted inner join lr on lr.lrstamp=inserted.lrstamp inner join cl on lr.no=cl.no and lr.estab=cl.estab where inserted.currec=1 and inserted.cm=21 and (inserted.valor<>0 or inserted.evalor<>0) else UPDATE CC SET cc.usrdata=inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis,cc.datalc=lr.sdata,cc.dataven=lr.ccdata,cc.fref=lr.fref,cc.intid=lr.intid,cc.ccusto=lr.ccusto,cc.ncusto=lr.ncusto from inserted inner join lr on lr.lrstamp=inserted.lrstamp where cc.lmstamp=inserted.lmstamp and inserted.cm=21 and inserted.currec=1if (Select count(*) from inserted where inserted.cm=18 or inserted.cm=11 or inserted.cm=26 or inserted.cm=13 or inserted.cm=3 or inserted.cm=5 or inserted.cm=23)>0 if (Select count(*) from ol,inserted where ol.lmstamp=inserted.lmstamp and (inserted.cm=18 orinserted.cm=11 or inserted.cm=26 or inserted.cm=13 or inserted.cm=3 or inserted.cm=5 or inserted.cm=23))=0 INSERT INTO OL (lrstamp,lmstamp,olstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,moeda) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LR',lr.nomeserie+' nº '+ltrim(str(inserted.letra,10,0)),inserted.cheque,inserted.badata,inserted.badata,case when inserted.cm=23 then case when (inserted.despesas+inserted.juros + inserted.iselo)>0 then 0 else (inserted.despesas+inserted.juros + inserted.iselo)*-1 end else case when inserted.cm<>26 then case when ((inserted.valor)-(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end)))>0 then ((inserted.valor)-(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end))) else 0 end else 0 end end,case when inserted.cm=23 then case when (inserted.edespesas+inserted.ejuros + inserted.eiselo)>0 then 0 else (inserted.edespesas+inserted.ejuros + inserted.eiselo)*-1 end else case when inserted.cm<>26 then case when ((inserted.evalor)-(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end)))>0 then ((inserted.evalor)-(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end))) else 0 end else 0 end end,case when inserted.cm=23 then case when (inserted.despesas+inserted.juros + inserted.iselo)>0 then (inserted.despesas+inserted.juros + inserted.iselo) else 0 end else case when inserted.cm<>26 then case when ((inserted.valor)-(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end)))<0 then (((inserted.valor)*-1)+(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end))) else 0 end else (inserted.valor)+inserted.despesas+inserted.juros + inserted.iselo end end,case when inserted.cm=23 then case when (inserted.edespesas+inserted.ejuros + inserted.eiselo)>0 then (inserted.edespesas+inserted.ejuros + inserted.eiselo) else 0 end else case when inserted.cm<>26 then case when ((inserted.evalor)-(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end)))<0 then (((inserted.evalor)*-1)+(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end))) else 0 end else (inserted.evalor)+inserted.edespesas+inserted.ejuros + inserted.eiselo end end,ltrim(rtrim(lr.nome))+'-'+inserted.lmdesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lr.fref,lr.intid,lr.ccusto,lr.ncusto,inserted.ollocal,inserted.contado,inserted.localol, 'PTE ou EURO' from inserted inner join lr on lr.lrstamp=inserted.lrstamp inner join oc on oc.olcodigo='R00001' where (inserted.cm=11 or inserted.cm=26 or inserted.cm=13 or inserted.cm=3 or inserted.cm=5 or inserted.cm=23 or inserted.cm=18) and inserted.currec=1 else UPDATE OL SET ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.data=inserted.badata,ol.dvalor=inserted.badata,ol.descricao=ltrim(rtrim(lr.nome))+'-'+inserted.lmdesc,ol.fref=lr.fref,ol.intid=lr.intid,ol.ccusto=lr.ccusto,ol.ncusto=lr.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.localol, ol.moeda='PTE ou EURO' from inserted inner join lr on lr.lrstamp=inserted.lrstamp where ol.lmstamp=inserted.lmstamp and (inserted.cm=11 or inserted.cm=26 or inserted.cm=13 or inserted.cm=3 or inserted.cm=5 or inserted.cm=23 or inserted.cm=18) and inserted.currec=1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_po_insert ON po WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONinsert into FC
(fcstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis, formapag,situacao, datalc,no,nome,cm,cmdesc,origem,difcambio,edifcambio
, deb,edeb,debf,edebf,debm,debfm, cred,ecred,credf,ecredf,credm,credfm
, moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs

)
select inserted.postamp
, inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
, '1','2', inserted.procdata,inserted.no,inserted.nome,inserted.cm,inserted.cmdesc,'PO', inserted.difcambio,inserted.edifcambio
, case when inserted.total<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.total<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.total>0 then 0 else -inserted.total-inserted.arred end
, case when inserted.etotal>0 then 0 else -inserted.etotal-inserted.earred end
, case when inserted.total>0 then 0 else -inserted.total-inserted.arred end
, case when inserted.etotal>0 then 0 else -inserted.etotal-inserted.earred end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, inserted.moeda,inserted.adoc,inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab
, inserted.pais,inserted.zona, inserted.desc1

from inserted
where inserted.process =1

-- desconto financeiro
insert into FC
(fcstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis, formapag,situacao, datalc,no,nome,cm,cmdesc,origem
, deb,edeb,debf,edebf,debm,debfm, cred,ecred,credf,ecredf,credm,credfm
, moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs)
select substring('F'+inserted.postamp,1,25)
, inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, '1','2'
, inserted.procdata,inserted.no,inserted.nome, 8, (select cmdesc from cm1 (nolock) where cm =8), 'PO'
, case when inserted.finv<0 then 0 else inserted.finv end
, case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finv<0 then 0 else inserted.finv end
, case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finv>0 then 0 else -inserted.finv end
, case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finv>0 then 0 else -inserted.finv end
, case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, inserted.moeda,inserted.adoc,inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab,inserted.pais
, inserted.zona, inserted.desc1
from inserted
where inserted.process=1 and inserted.finv<>0

-- Valor de IRS
insert into fc
(fcstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis, formapag,situacao, datalc,no,nome,cm,cmdesc,origem
, deb, edeb, debf, edebf, cred, ecred, credf, ecredf
, moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs)
select substring('I'+inserted.postamp,1,25)
, inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
, '1','2', inserted.procdata,inserted.no,inserted.nome, 0, 'Indefinido!', 'PO'
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, 'PTE ou EURO',inserted.adoc,inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo
, inserted.estab, inserted.pais,inserted.zona, inserted.desc1
from inserted
where inserted.process=1 and (inserted.virs<>0 or inserted.evirs<>0)

-- Tesouraria
insert into OL
(olstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque, data,dvalor,entr,eentr,entrm,said,esaid
, saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,intid,ollocal,contado,local, site,pnome,pno,cxstamp,cxusername,ssstamp
, ssusername, operext)
select inserted.postamp
, inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'PO'
, inserted.cmdesc,inserted.adoc, inserted.procdata,inserted.dvalor
, Case When case when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow else inserted.total-inserted.totol2+inserted.totow
end > 0 THEN 0 else case when memissao='EURO' then (inserted.total-inserted.totol2-inserted.arred+inserted.totow)*-1 else (inserted.total-inserted.totol2+inserted.totow)*-1
end end
, Case When case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow else inserted.etotal-inserted.etotol2+inserted.etotow
end > 0 THEN 0 ELSE case when inserted.memissao='PTE ' then (inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow)*-1 else (inserted.etotal-inserted.etotol2+inserted.etotow)*-1
end End
, Case When inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 > 0 THEN 0 else (inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2)*-1 End
, Case When case when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow else inserted.total-inserted.totol2+inserted.totow
end < 0 Then 0 else	case when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow else inserted.total-inserted.totol2+inserted.totow
end end
, Case When case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow else inserted.etotal-inserted.etotol2+inserted.etotow
end < 0  THEN 0 ELSE case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow else inserted.etotal-inserted.etotol2+inserted.etotow
end End
, Case When inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 < 0 THEN 0 else inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 End
, inserted.moeda2, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.ollocal,inserted.contado
,inserted.telocal, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername, inserted.ssstamp,inserted.ssusername,inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo where inserted.process=1

insert into OL 
(olstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque, data,dvalor,said,esaid,saidm,moeda,descricao
, olcodigo,grupo,sgrupo,fref,ccusto,ncusto,intid,ollocal,contado,local, site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select substring('2'+inserted.postamp,1,25)
, substring('2'+inserted.postamp,1,25), inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'PO'
, inserted.cmdesc,inserted.adoc, inserted.procdata,inserted.dvalor,inserted.totol2,inserted.etotol2, inserted.valor2m3,inserted.moeda3
, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.ollocal2
, inserted.contado2,inserted.telocal2, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername
, inserted.ssstamp,inserted.ssusername,inserted.operext
from inserted 
inner join oc on oc.olcodigo=inserted.olcodigo 
where inserted.process=1 and (inserted.totol2<>0 or inserted.etotol2<>0)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_po_update ON po WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128)
SELECT @Cinfo = Context_Info()
IF @Cinfo = 0x22222 RETURN

if update(usrhora)
or update(no) or update(nome) or update(total) or  update(etotal) or update(procdata) or update(difcambio) or update(edifcambio)
or update(total) or update(arred) or update(etotal) or update(earred) or update(moeda) or update(rno) or update(procdata) or update(fref)
or update(intid) or update(ccusto) or update(ncusto) or update(zona) or update(tipo) or update(pais) or update(estab) or update(desc1)
or update(totol2) or update(etotol2) or update(ollocal) or update(contado) or update(ollocal2) or update(contado2) or update(telocal)
or update(telocal2) or update(olcodigo) or update(finv) or update(process)
begin
insert into FC
(fcstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis, datalc,no,nome,cm,cmdesc,origem,difcambio,edifcambio
, deb,edeb,debf,edebf,debm,debfm, cred,ecred,credf,ecredf,credm,credfm
, moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs

)
select inserted.postamp,inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
, inserted.procdata,inserted.no,inserted.nome,inserted.cm,inserted.cmdesc,'PO', inserted.difcambio,inserted.edifcambio
, case when inserted.total<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.total<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.total>0 then 0 else -inserted.total-inserted.arred end
, case when inserted.etotal>0 then 0 else -inserted.etotal-inserted.earred end
, case when inserted.total>0 then 0 else -inserted.total-inserted.arred end
, case when inserted.etotal>0 then 0 else -inserted.etotal-inserted.earred end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, inserted.moeda,inserted.adoc,inserted.procdata,inserted.fref,inserted.ccusto
,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab,inserted.pais
,inserted.zona, inserted.desc1

from inserted
where inserted.process=1 and inserted.postamp not in (select fc.fcstamp from fc where fc.fcstamp=inserted.postamp)

update FC
set fc.fcstamp =inserted.postamp, fc.postamp=inserted.postamp
, fc.usrdata =inserted.usrdata,fc.usrhora=inserted.usrhora,fc.usrinis=inserted.usrinis
, fc.datalc =inserted.procdata,fc.no=inserted.no,fc.nome=inserted.nome,fc.cm=inserted.cm
, fc.cmdesc =inserted.cmdesc,fc.origem='PO'
, fc.difcambio =inserted.difcambio,fc.edifcambio=inserted.edifcambio
, fc.deb =case when inserted.total<0 then 0 else inserted.total-inserted.arred end
, fc.edeb =case when inserted.etotal<0 then 0 else inserted.etotal-inserted.earred end
, fc.debf =case when inserted.total<0 then 0 else inserted.total-inserted.arred end
, fc.edebf =case when inserted.etotal<0 then 0 else inserted.etotal-inserted.earred end
, fc.debm =case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, fc.debfm =case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, fc.cred =case when inserted.total>0 then 0 else -inserted.total-inserted.arred end
, fc.ecred =case when inserted.etotal>0 then 0 else -inserted.etotal-inserted.earred end
, fc.credf =case when inserted.total>0 then 0 else -inserted.total-inserted.arred end
, fc.ecredf =case when inserted.etotal>0 then 0 else -inserted.etotal-inserted.earred end
, fc.credm =case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, fc.credfm =case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, fc.moeda =inserted.moeda, fc.adoc=inserted.adoc,fc.dataven=inserted.procdata, fc.fref=inserted.fref,fc.ccusto=inserted.ccusto,fc.ncusto=inserted.ncusto
, fc.intid =inserted.intid, fc.tipo=inserted.tipo,fc.estab=inserted.estab,fc.pais=inserted.pais,fc.zona=inserted.zona,fc.obs=inserted.desc1

from inserted
where inserted.process =1
and inserted.postamp in (select fc.fcstamp from fc where fc.fcstamp=inserted.postamp)  and inserted.postamp=fc.fcstamp

delete from fc
from inserted
where inserted.process=0 and inserted.postamp in (select fc.fcstamp from fc where fc.fcstamp=inserted.postamp) and inserted.postamp=fc.fcstamp

--  Desconto Financeiro
insert into FC
(fcstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis, datalc,no,nome,cm,cmdesc,origem, deb,edeb,debf,edebf,debm,debfm
, cred,ecred,credf,ecredf,credm,credfm, moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs)
select substring('F'+inserted.postamp,1,25)
, inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, inserted.procdata
,inserted.no, inserted.nome, 8, (select cmdesc from cm1 (nolock) where cm =8), 'PO'
, case when inserted.finv<0 then 0 else inserted.finv end
, case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finv<0 then 0 else inserted.finv end
, case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finv>0 then 0 else -inserted.finv end
, case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finv>0 then 0 else -inserted.finv end
, case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, inserted.moeda,inserted.adoc,inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab
,inserted.pais,inserted.zona, inserted.desc1
from inserted
where inserted.process=1
and substring('F'+inserted.postamp,1,25) not in (select fc.fcstamp from fc where fc.fcstamp=substring('F'+inserted.postamp,1,25)) and inserted.finv<>0

update FC
set fc.fcstamp =substring('F'+inserted.postamp,1,25),fc.postamp=inserted.postamp
, fc.usrdata=inserted.usrdata,fc.usrhora=inserted.usrhora,fc.usrinis=inserted.usrinis
, fc.datalc=inserted.procdata,fc.no=inserted.no,fc.nome=inserted.nome
, fc.cm =8
, fc.cmdesc =(select cmdesc from cm1 (nolock) where cm =8)
, fc.origem ='PO'
, fc.deb =case when inserted.finv<0 then 0 else inserted.finv end,fc.edeb=case when inserted.efinv<0 then 0 else inserted.efinv end
, fc.debf =case when inserted.finv<0 then 0 else inserted.finv end,fc.edebf=case when inserted.efinv<0 then 0 else inserted.efinv end
, fc.debm =case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end,fc.debfm=case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, fc.cred =case when inserted.finv>0 then 0 else -inserted.finv end,fc.ecred=case when inserted.efinv>0 then 0 else -inserted.efinv end
, fc.credf =case when inserted.finv>0 then 0 else -inserted.finv end,fc.ecredf=case when inserted.efinv>0 then 0 else -inserted.efinv end
, fc.credm =case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end,fc.credfm=case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, fc.moeda =inserted.moeda,fc.adoc=inserted.adoc,fc.dataven=inserted.procdata
, fc.fref =inserted.fref,fc.ccusto=inserted.ccusto,fc.ncusto=inserted.ncusto,fc.intid=inserted.intid,fc.tipo=inserted.tipo,fc.estab=inserted.estab
, fc.pais =inserted.pais,fc.zona=inserted.zona,fc.obs=inserted.desc1
from inserted
where inserted.process=1
and substring('F'+inserted.postamp,1,25) in (select fc.fcstamp from fc where fc.fcstamp=substring('F'+inserted.postamp,1,25))  and substring('F'+inserted.postamp,1,25)=fc.fcstamp and inserted.finv<>0

-- IRS
insert into FC
(fcstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis, datalc,no,nome,cm,cmdesc
, origem, deb,edeb,debf,edebf,cred,ecred,credf,ecredf
, moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs)
select substring('I'+inserted.postamp,1,25)
, inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, inserted.procdata
, inserted.no,inserted.nome, 0, 'Indefinido!', 'PO'
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, 'PTE ou EURO', inserted.adoc, inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo
, inserted.estab,inserted.pais, inserted.zona, inserted.desc1
from inserted
where inserted.process=1
and (substring('I'+inserted.postamp,1,25) not in (select fc.fcstamp from fc where fc.fcstamp=substring('I'+inserted.postamp,1,25))) and (inserted.virs<>0 or inserted.evirs<>0)

update FC
set fc.fcstamp =substring('I'+inserted.postamp,1,25),fc.postamp=inserted.postamp
, fc.usrdata =inserted.usrdata,fc.usrhora=inserted.usrhora,fc.usrinis=inserted.usrinis
, fc.datalc =inserted.procdata,fc.no=inserted.no,fc.nome=inserted.nome
, fc.cm =0, fc.cmdesc ='Indefinido!', fc.origem='PO'
, fc.deb =case when inserted.virs<0 then 0 else inserted.virs end,fc.edeb=case when inserted.evirs<0 then 0 else inserted.evirs end
, fc.debf =case when inserted.virs<0 then 0 else inserted.virs end,fc.edebf=case when inserted.evirs<0 then 0 else inserted.evirs end
, fc.cred =case when inserted.virs>0 then 0 else -inserted.virs end,fc.ecred=case when inserted.evirs>0 then 0 else -inserted.evirs end
, fc.credf =case when inserted.virs>0 then 0 else -inserted.virs end,fc.ecredf=case when inserted.evirs>0 then 0 else -inserted.evirs end
, fc.moeda ='PTE ou EURO', fc.adoc=inserted.adoc,fc.dataven=inserted.procdata
, fc.fref =inserted.fref,fc.ccusto=inserted.ccusto,fc.ncusto=inserted.ncusto,fc.intid=inserted.intid,fc.tipo=inserted.tipo,fc.estab=inserted.estab
, fc.pais =inserted.pais,fc.zona=inserted.zona,fc.obs=inserted.desc1
from inserted
where inserted.process=1
and substring('I'+inserted.postamp,1,25) in (select fc.fcstamp from fc where fc.fcstamp=substring('I'+inserted.postamp,1,25))  and substring('I'+inserted.postamp,1,25)=fc.fcstamp
and (inserted.virs<>0 or inserted.evirs<>0)

-- Tesouraria
delete from OL from inserted where inserted.process=0 and inserted.postamp=ol.postamp

delete from OL from inserted where (inserted.process=0 or (inserted.totol2=0 and inserted.etotol2=0)) and substring('2'+inserted.postamp,1,25)=ol.postamp

IF update(usrdata) or update(usrhora) or update(usrinis) or update(ousrdata) or update(ousrhora) or update(ousrinis)
or update(cmdesc) or update(adoc) or update (procdata) or update (dvalor) or update (total)  or update (totol2)  or update (arred) or update (totow)
or update (etotal) or update (etotol2) or update (earred) or update (etotow) or update (valorm2) or update (valor2m2) or update (earredm2) or update (valorowm2)
or update (moeda2) or update (nome) or update (olcodigo)  or update (fref) or update (ccusto) or update (ncusto) or update (intid)
or update (ollocal) or update (contado) or update (telocal) or update (site) or update (pnome) or update (pno) or update (ssusername)
begin
UPDATE OL
SET ol.olstamp =inserted.postamp,ol.postamp=inserted.postamp
, ol.usrdata =inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis
, ol.ousrdata =inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='PO'
, ol.documento =inserted.cmdesc,ol.cheque=inserted.adoc
, ol.data =inserted.procdata,ol.dvalor=inserted.dvalor
, ol.entr =case when 	
case when memissao ='EURO'
then inserted.total-inserted.totol2-inserted.arred+inserted.totow else inserted.total-inserted.totol2+inserted.totow  end > 0
then 0 else
case when memissao='EURO' then (inserted.total-inserted.totol2-inserted.arred+inserted.totow)*-1 else (inserted.total-inserted.totol2+inserted.totow)*-1
end end
, ol.eentr =Case When case
when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow
end > 0 then 0
else case when inserted.memissao='PTE ' then (inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow)*-1 else (inserted.etotal-inserted.etotol2+inserted.etotow)*-1
end End
, ol.entrm =Case
when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 > 0 THEN 0
else (inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2)*-1
end
, ol.said =Case when case
when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow
else inserted.total-inserted.totol2+inserted.totow
end < 0
Then 0
else case
when memissao ='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow
else inserted.total-inserted.totol2+inserted.totow
end end
, esaid =Case When case
when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow
end < 0  THEN 0 ELSE
case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow
end End
, ol.saidm =Case
When inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 < 0 THEN 0
else inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 End
, ol.moeda =inserted.moeda2
, ol.descricao=inserted.nome, ol.olcodigo=inserted.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo
, ol.fref =inserted.fref, ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.intid=inserted.intid,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado
, ol.local =inserted.telocal, ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp, ol.ssstamp=inserted.ssstamp
, ol.ssusername =inserted.ssusername
, ol.operext =inserted.operext
from inserted
inner join oc on oc.olcodigo =inserted.olcodigo
where ol.postamp=inserted.postamp and inserted.process=1
END

If update(usrdata) or update(usrhora) or update(usrinis) or update(ousrdata) or update(ousrhora) or update(ousrinis) or update(cmdesc) or update(adoc)
or update(procdata) or update(dvalor) or update(totol2) or update(etotol2) or update(valor2m3) or update(moeda3) or update(nome) or update(olcodigo)
or update(fref) or update(ccusto) or update(ncusto) or update(intid) or update(ollocal2) or update(contado2) or update(telocal2)
or update(site) or update(pnome) or update(pno) or update(ssusername)
BEGIN
update OL
set ol.olstamp =substring('2'+inserted.postamp,1,25),ol.postamp=substring('2'+inserted.postamp,1,25)
, ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis
, ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='PO'
, ol.documento=inserted.cmdesc,ol.cheque=inserted.adoc
, ol.data=inserted.procdata,ol.dvalor=inserted.dvalor,ol.said=inserted.totol2,ol.esaid=inserted.etotol2
, ol.saidm=inserted.valor2m3 , ol.moeda=inserted.moeda3, ol.descricao=inserted.nome,ol.olcodigo=inserted.olcodigo
, ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo, ol.fref=inserted.fref,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto
, ol.intid=inserted.intid,ol.ollocal=inserted.ollocal2,ol.contado=inserted.contado2,ol.local=inserted.telocal2
, ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp
, ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where ol.postamp=substring('2'+inserted.postamp,1,25) and inserted.process=1
END

insert into OL
(olstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque, data,dvalor,entr,eentr,entrm
,said,esaid,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,intid,ollocal,contado,local, site,pnome,pno,cxstamp,cxusername
,ssstamp,ssusername,operext)
select inserted.postamp
,inserted.postamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
, 'PO', inserted.cmdesc,inserted.adoc, inserted.procdata,inserted.dvalor
, Case When case
when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow
else inserted.total-inserted.totol2+inserted.totow
end > 0 THEN 0 else case when memissao='EURO' then (inserted.total-inserted.totol2-inserted.arred+inserted.totow)*-1
else (inserted.total-inserted.totol2+inserted.totow)*-1
end end
, Case When case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow
end > 0 THEN 0 ELSE case when inserted.memissao='PTE ' then (inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow)*-1
else (inserted.etotal-inserted.etotol2+inserted.etotow)*-1
end End
, Case When inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 > 0 THEN 0
else (inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2)*-1 End
, Case When case when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow
else inserted.total-inserted.totol2+inserted.totow
end < 0 Then 0 else	case when memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow
else inserted.total-inserted.totol2+inserted.totow
end end
, Case When case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow
end < 0  THEN 0 ELSE case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow
end End
, Case When inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 < 0 THEN 0
else inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2
End
, inserted.moeda2
, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.ollocal,inserted.contado,inserted.telocal
, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername, inserted.ssstamp,inserted.ssusername,inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where inserted.process =1 and inserted.postamp not in (select ol.postamp from ol where ol.postamp=inserted.postamp)

insert into OL
(olstamp,postamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque, data,dvalor,said,esaid,saidm,moeda,descricao
, olcodigo,grupo,sgrupo,fref,ccusto,ncusto,intid,ollocal,contado,local, site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select substring('2'+inserted.postamp,1,25)
, substring('2'+inserted.postamp,1,25), inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
, 'PO', inserted.cmdesc,inserted.adoc, inserted.procdata,inserted.dvalor,inserted.totol2,inserted.etotol2, inserted.valor2m3,inserted.moeda3, inserted.nome
, inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.ollocal2,inserted.contado2
, inserted.telocal2, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername, inserted.ssstamp,inserted.ssusername,inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where inserted.process=1
and (inserted.totol2<>0 or inserted.etotol2<>0) and substring('2'+inserted.postamp,1,25) not in (select ol.postamp from ol where ol.postamp=substring('2'+inserted.postamp,1,25))

delete from fc 
from inserted 
where inserted.process =0 
and ((substring('F'+inserted.postamp,1,25) in (select fc.fcstamp from fc where fc.fcstamp=substring('F'+inserted.postamp,1,25)) and substring('F'+inserted.postamp,1,25)=fc.fcstamp)
or (substring('I'+inserted.postamp,1,25) in (select fc.fcstamp from fc where fc.fcstamp=substring('I'+inserted.postamp,1,25)) and substring('I'+inserted.postamp,1,25)=fc.fcstamp)
or (substring('C'+inserted.postamp,1,25) in (select fc.fcstamp from fc where fc.fcstamp=substring('C'+inserted.postamp,1,25)) and substring('C'+inserted.postamp,1,25)=fc.fcstamp)
or (substring('E'+inserted.postamp,1,25) in (select fc.fcstamp from fc where fc.fcstamp=substring('E'+inserted.postamp,1,25)) and substring('E'+inserted.postamp,1,25)=fc.fcstamp))ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rd_update ON RD WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinford VARBINARY(128) SELECT @Cinford = Context_Info() IF @Cinford = 0x77777 return UPDATE CC SET cc.ccstamp=inserted.rdstamp,cc.rdstamp=inserted.rdstamp,cc.usrdata=inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis,cc.datalc=inserted.rdata,cc.no=inserted.no,cc.nome=inserted.nome,cc.cm=inserted.cm,cc.cmdesc=inserted.cmdesc,cc.origem='RD',cc.cobranca=inserted.cobranca, cc.cred=case when tsrd.regrd = 0 then inserted.total else 0 end,  cc.ecred=case when tsrd.regrd = 0 then inserted.etotal else 0 end,  cc.credm=case when tsrd.regrd = 0 then inserted.totalmoeda else 0 end,  cc.deb=case when tsrd.regrd = 1 then inserted.total*-1 else 0 end,  cc.debf=case when tsrd.regrd = 1 then inserted.total*-1 else 0 end,  cc.edeb=case when tsrd.regrd = 1 then inserted.etotal*-1 else 0 end,  cc.edebf=case when tsrd.regrd = 1 then inserted.etotal*-1 else 0 end,   cc.debm=case when tsrd.regrd = 1 then inserted.totalmoeda*-1 else 0 end,  debfm=case when tsrd.regrd = 1 then inserted.totalmoeda*-1 else 0 end,  cc.formapag=inserted.formapag,cc.ivav1=case when inserted.tabiva=1 then inserted.ivav else 0 end,cc.ivav2=case when inserted.tabiva=2 then inserted.ivav else 0 end,cc.ivav3=case when inserted.tabiva=3 then inserted.ivav else 0 end,cc.ivav4=case when inserted.tabiva=4 then inserted.ivav else 0 end,cc.ivav5=case when inserted.tabiva=5 then inserted.ivav else 0 end,cc.ivav6=case when inserted.tabiva=6 then inserted.ivav else 0 end,cc.ivav7=case when inserted.tabiva=7 then inserted.ivav else 0 end,cc.ivav8=case when inserted.tabiva=8 then inserted.ivav else 0 end,cc.ivav9=case when inserted.tabiva=9 then inserted.ivav else 0 end,cc.eivav1=case when inserted.tabiva=1 then inserted.eivav else 0 end,cc.eivav2=case when inserted.tabiva=2 then inserted.eivav else 0 end,cc.eivav3=case when inserted.tabiva=3 then inserted.eivav else 0 end,cc.eivav4=case when inserted.tabiva=4 then inserted.eivav else 0 end,cc.eivav5=case when inserted.tabiva=5 then inserted.eivav else 0 end,cc.eivav6=case when inserted.tabiva=6 then inserted.eivav else 0 end,cc.eivav7=case when inserted.tabiva=7 then inserted.eivav else 0 end,cc.eivav8=case when inserted.tabiva=8 then inserted.eivav else 0 end,cc.eivav9=case when inserted.tabiva=9 then inserted.eivav else 0 end,cc.moeda=inserted.moeda,cc.nrdoc=inserted.rno,cc.dataven=inserted.rdata,cc.fref=inserted.fref,cc.intid=inserted.intid,cc.cobrador=inserted.cobrador,cc.rota=inserted.rota,cc.ccusto=inserted.ccusto,cc.ncusto=inserted.ncusto,cc.zona=inserted.zona,cc.vendedor=inserted.vendedor,cc.vendnm=inserted.vendnm,cc.segmento=inserted.segmento,cc.tipo=inserted.tipo,cc.pais=inserted.pais,cc.estab=inserted.estab,cc.obs=inserted.descricao, cc.cambiofixo = inserted.cambiofixo , cc.eivacativado = -inserted.eivacativado, 						cc.ivacativado = -inserted.ivacativado, 						cc.mivacativado = -inserted.mivacativado from inserted inner join tsrd on tsrd.ndoc = inserted.ndoc where inserted.rdstamp=cc.ccstamp  if (select isnull(count(*),0) from deleted inner join tsrd on tsrd.ndoc = deleted.ndoc where tsrd.regrd = 1) > 0  BEGIN  update cc  set cc.credf = cc.credf - (deleted.total*-1)  , cc.ecredf = cc.ecredf  - (deleted.etotal*-1)  , cc.credfm =cc.credfm - (deleted.totalmoeda*-1)  from deleted,cc	where deleted.ordstamp = cc.rdstamp  update cc  set cc.credf = cc.credf + (inserted.total*-1)  , cc.ecredf = cc.ecredf  + (inserted.etotal*-1)  , cc.credfm =cc.credfm + (inserted.totalmoeda*-1)  from inserted,cc	where inserted.ordstamp = cc.rdstamp  END UPDATE OL SET ol.olstamp=inserted.rdstamp,ol.rdstamp=inserted.rdstamp,ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='RD',ol.documento=(case when (select oldoc from tsrd where ndoc=inserted.ndoc)=' ' then inserted.cmdesc else (select oldoc from tsrd where ndoc=inserted.ndoc) end),ol.cheque=convert(char(15),inserted.rno),ol.data=inserted.rdata,ol.dvalor=inserted.rdata, ol.entr=case when tsrd.regrd = 0 then inserted.total - inserted.ivacativado else 0 end,  ol.eentr=case when tsrd.regrd = 0 then inserted.etotal - inserted.eivacativado else 0 end,  ol.entrm=case when tsrd.regrd = 0 then inserted.valorm2 - inserted.mivacativado else 0 end,  ol.said=case when tsrd.regrd = 1 then inserted.total*-1 + inserted.ivacativado else 0 end,   ol.esaid=case when tsrd.regrd = 1 then inserted.etotal*-1 + inserted.eivacativado else 0 end,   ol.saidm=case when tsrd.regrd = 1 then inserted.valorm2*-1 + inserted.mivacativado else 0 end,  ol.moeda=inserted.moeda2,ol.descricao=inserted.nome,ol.olcodigo=inserted.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo,ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.telocal,ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp,ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=inserted.operext,ol.cecope=inserted.cecope from inserted inner join oc on oc.olcodigo=inserted.olcodigo inner join tsrd on  tsrd.ndoc = inserted.ndoc where ol.rdstamp=inserted.rdstamp UPDATE CH SET ch.chstamp=inserted.rdstamp,ch.rdstamp=inserted.rdstamp,ch.usrdata=inserted.usrdata,ch.usrhora=inserted.usrhora,ch.usrinis=inserted.usrinis,ch.data=inserted.chdata,ch.no=inserted.no,ch.nome=inserted.nome,ch.estab=inserted.estab,ch.origem='RD',ch.valor=inserted.total-inserted.ivacativado,ch.evalor=inserted.etotal-inserted.eivacativado,ch.valorm=inserted.valorm2-inserted.mivacativado,ch.moeda=inserted.moeda2,ch.ollocal=inserted.ollocal,ch.clbanco=inserted.clbanco,ch.clcheque=inserted.clcheque, ch.fref=inserted.fref,ch.tptit=inserted.tptit from inserted where inserted.cheque=1 and inserted.rdstamp=ch.rdstamp and inserted.rdstamp = ch.chstampINSERT INTO CH (chstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,clbanco,clcheque, fref,tptit, contado, ollocal) select inserted.rdstamp,inserted.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.chdata,inserted.ousrdata,inserted.ousrdata,inserted.no,inserted.nome,inserted.estab,'RD',inserted.total-inserted.ivacativado,inserted.etotal-inserted.eivacativado,inserted.valorm2-inserted.mivacativado,inserted.moeda2,inserted.clbanco,inserted.clcheque, inserted.fref,inserted.tptit, inserted.contado, inserted.ollocal from inserted where inserted.cheque=1 and not exists (select rdstamp from ch where ch.rdstamp=inserted.rdstamp) and inserted.anulado=0 DELETE FROM CH FROM inserted where inserted.cheque=0 and inserted.rdstamp in (select ch.chstamp from ch where ch.rdstamp=inserted.rdstamp) and inserted.rdstamp=ch.chstamp IF update(anulado) Begin DELETE FROM CC from inserted where inserted.anulado=1 and cc.rdstamp=inserted.rdstamp DELETE FROM CH from inserted where inserted.anulado=1 and ch.rdstamp=inserted.rdstamp DELETE FROM OL from inserted where inserted.anulado=1 and ol.rdstamp=inserted.rdstamp if (select isnull(count(*),0) from inserted inner join tsrd on tsrd.ndoc = inserted.ndoc where tsrd.regrd = 1) > 0  BEGIN  update cc  set cc.credf = cc.credf - (inserted.total*-1)  , cc.ecredf = cc.ecredf  - (inserted.etotal*-1)  , cc.credfm =cc.credfm - (inserted.totalmoeda*-1)  from inserted,cc	where inserted.ordstamp = cc.rdstamp  END End  IF (SELECT COUNT(inserted.RDstamp) from inserted inner join deleted on deleted.RDstamp=inserted.RDstamp where inserted.anulado = 1 AND deleted.anulado = 0) > 0  BEGIN UPDATE RD SET RD.arquivadodigital = 0  FROM inserted  INNER JOIN deleted ON deleted.RDstamp=inserted.RDstamp  INNER JOIN RD ON RD.RDstamp=inserted.RDstamp  WHERE inserted.anulado=1 AND deleted.anulado=0  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_re_insert ON re WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO CC
(ccstamp,restamp,facstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,formapag,situacao
, datalc,no,nome,cm,cmdesc,origem,cobranca,difcambio,edifcambio
, cred,ecred,credf,ecredf,credm,credfm,deb,edeb,debf,edebf,debm,debfm
, moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs,incobra

)
select inserted.restamp,inserted.restamp,inserted.facstamp, inserted.usrdata,inserted.usrhora,inserted.usrinis
, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'1','2',inserted.procdata,inserted.no,inserted.nome
, (case when exists (select cm from cm1 (nolock) where cm=(select cmcc from tsre (nolock) where ndoc=inserted.ndoc)) then (select cmcc from tsre (nolock) where ndoc=inserted.ndoc)
else 70 end)
, (case when exists (select cmdesc from cm1 (nolock) where cm=(select cmcc from tsre (nolock) where ndoc=inserted.ndoc)) then (select cmccn from tsre (nolock) where ndoc=inserted.ndoc)
else (select cmdesc from cm1 (nolock) where cm=70) end)
, 'RE',inserted.cobranca,inserted.difcambio,inserted.edifcambio
, case when inserted.total-inserted.arred<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal-inserted.earred<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.total-inserted.arred<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal-inserted.earred<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.total-inserted.arred>0 then 0 else -(inserted.total-inserted.arred) end
, case when inserted.etotal-inserted.earred>0 then 0 else -(inserted.etotal-inserted.earred) end
, case when inserted.total-inserted.arred>0 then 0 else -(inserted.total-inserted.arred) end
, case when inserted.etotal-inserted.earred>0 then 0 else -(inserted.etotal-inserted.earred) end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, inserted.moeda,inserted.rno,inserted.procdata,inserted.fref,inserted.intid,inserted.cobrador,inserted.rota,inserted.ccusto
,inserted.ncusto,inserted.zona,inserted.vendedor,inserted.vendnm,inserted.segmento,inserted.tipo,inserted.pais,inserted.estab,inserted.desc1
, (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)

from inserted
where inserted.process=1

INSERT INTO CC
(ccstamp,restamp,facstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,formapag,situacao, datalc,no,nome,cm,cmdesc,origem,cobranca
, cred,ecred,credf,ecredf,credm,credfm,deb,edeb,debf,edebf,debm,debfm
, moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs)
select substring('F'+inserted.restamp,1,25),inserted.restamp,inserted.facstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis
, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, '1','2', inserted.procdata,inserted.no,inserted.nome, 94
, (select cmdesc from cm1 (nolock) where cm =94),'RE',inserted.cobranca
, case when inserted.finv<0 then 0 else inserted.finv end
, case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finv<0 then 0 else inserted.finv end
, case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finv>0 then 0 else -inserted.finv end
, case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finv>0 then 0 else -inserted.finv end
, case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, inserted.moeda,inserted.rno,inserted.procdata,inserted.fref,inserted.intid,inserted.cobrador,inserted.rota,inserted.ccusto,inserted.ncusto,inserted.zona
,inserted.vendedor,inserted.vendnm, inserted.segmento,inserted.tipo,inserted.pais,inserted.estab,inserted.desc1
from inserted
where inserted.process=1 and inserted.finv<>0

INSERT INTO CC
(ccstamp,restamp,facstamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,formapag,situacao,datalc,no,nome,cm,cmdesc,origem
, cred,ecred,credf,ecredf,deb,edeb,debf,edebf
, moeda,nrdoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs)
select substring('I'+inserted.restamp,1,25),inserted.restamp,inserted.facstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis
,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, '1','2', inserted.procdata,inserted.no,inserted.nome, 0
, 'Indefinido!'
, 'RE'
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, inserted.moeda,inserted.rno,inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab
,inserted.pais,inserted.zona,inserted.desc1
from inserted
where inserted.process=1 and (inserted.virs<>0 or inserted.evirs<>0)

if (select count(*) from inserted where inserted.process=1
and 0 = (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)
and (case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end<>0
or case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end<>0
or inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2<>0)) >0

INSERT INTO OL
(olstamp,restamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque,data,dvalor,entr,eentr,entrm,said,esaid,saidm
, moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local, site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select inserted.restamp,inserted.restamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'RE'
, (case when (select oldoc from tsre where ndoc=inserted.ndoc)=' ' then 'Recibo' else (select oldoc from tsre where ndoc=inserted.ndoc) end)
, convert(char(15),inserted.rno),inserted.procdata,inserted.procdata
, case when case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow end<0 then 0
else case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end end
, case when case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end<0 then 0
else case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end end
, case when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2<0 then 0
else inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 end
, case when case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end>0 then 0
else -(case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end) end
, case when case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end>0 then 0
else -(case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end) end
, case when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2>0 then 0
else -(inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2) end
, inserted.moeda2, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto
, inserted.ollocal, inserted.contado,inserted.telocal, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername, inserted.ssstamp
, inserted.ssusername,inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where inserted.process=1

if (select count(*) from inserted where inserted.process=1
and 0 = (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)
and (inserted.totol2<>0 or inserted.etotol2<>0 or inserted.valor2m3<>0))>0

INSERT INTO OL
(olstamp,restamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem, documento,cheque,data,dvalor
, entr,eentr,entrm,said,esaid,saidm
, moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select substring('2'+inserted.restamp,1,25),substring('2'+inserted.restamp,1,25)
, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, 'RE'
, (case when (select oldoc from tsre where ndoc=inserted.ndoc)=' ' then 'Recibo' else (select oldoc from tsre where ndoc=inserted.ndoc) end)
, convert(char(15),inserted.rno)
, inserted.procdata,inserted.procdata
, case when inserted.totol2<0 then 0 else inserted.totol2 end
, case when inserted.etotol2<0 then 0 else inserted.etotol2 end
, case when inserted.valor2m3<0 then 0 else inserted.valor2m3 end
, case when inserted.totol2>0 then 0 else -inserted.totol2 end
, case when inserted.etotol2>0 then 0 else -inserted.etotol2 end
, case when inserted.valor2m3>0 then 0 else -inserted.valor2m3 end
, inserted.moeda3, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto
, inserted.ollocal2, inserted.contado2,inserted.telocal2, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername,inserted.ssstamp
, inserted.ssusername, inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where inserted.process=1 and (inserted.totol2<>0 or inserted.etotol2<>0)GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sl_insert ON SL WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONDECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTDECLARE @@STICLASTDATE datetimeSELECT @@STICLASTDATE = (select case when exists(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc)  then convert(datetime,(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc),121) else convert(datetime,'19000101',121) end) IF @@MROW = 1 BEGIN IF exists(select * from inserted where inserted.datalc < @@STICLASTDATE)RETURN END IF not EXISTS(select sa.ref from sa,inserted where sa.ref=inserted.ref and sa.armazem=inserted.armazem)INSERT INTO SA (sastamp,armazem,ref) select distinct convert(char(5),armazem)+ref,armazem,ref from inserted where not EXISTS(select ref from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and inserted.datalc >= @@STICLASTDATE IF not EXISTS(select sx.ref from sx,inserted where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam)INSERT INTO Sx (sxstamp,armazem,ref,cor,tam,ststamp) select distinct substring(cast(newid() as char(36)),1,25),inserted.armazem,inserted.ref,inserted.cor,inserted.tam,st.ststamp from inserted inner join st on inserted.ref=st.ref where inserted.cor!='' and inserted.tam!='' and not EXISTS(select armazem from sx where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam)  and inserted.datalc >= @@STICLASTDATE GROUP BY inserted.armazem,inserted.ref,inserted.cor,inserted.tam,st.ststamp IF (select count(*) from inserted where inserted.cm<50)>0 BEGINUPDATE ST SET st.pcpond=case when inserted.qtt+st.stock>0  and inserted.cm<50 and cm2.mudapcpond=1 then (st.pcpond*(case when st.stock>0 then st.stock else 0 end)+inserted.vu*inserted.qtt)/((case when st.stock>0 then st.stock else 0 end)+inserted.qtt) else st.pcpond end,st.epcpond=case when inserted.qtt+st.stock>0  and inserted.cm<50 and cm2.mudapcpond=1 then (st.epcpond*(case when st.stock>0 then st.stock else 0 end)+inserted.evu*inserted.qtt)/((case when st.stock>0 then st.stock else 0 end)+inserted.qtt) else st.epcpond end from INSERTED inner join cm2 on inserted.cm=cm2.cm where st.ref=inserted.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ENDIF @@MROW = 1BEGIN UPDATE ST SET st.stock=st.stock+case when inserted.cm<50 then inserted.qtt else -inserted.qtt end ,  st.uintr=case when inserted.datalc>=st.uintr  and inserted.cm<50 then inserted.datalc else st.uintr end,  st.pcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.vu else st.pcult end,  st.usaid=case when inserted.datalc>=st.usaid  and inserted.cm>50 then inserted.datalc else st.usaid end,  st.pvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50 and inserted.qtt>0 and inserted.origem='FT' then inserted.vu else st.pvultimo end,  st.epcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.evu else st.epcult end,  st.epvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50  and inserted.qtt>0 and inserted.origem='FT'  and inserted.origem='FT' then inserted.evu else st.epvultimo end,  st.qttacout=case when inserted.cm>50 then inserted.QTT+st.qttacout else st.qttacout end,  st.qttacin=case when inserted.cm<50 then inserted.QTT+st.qttacin else st.qttacin end,  st.udata=inserted.datalc  from INSERTED inner join cm2 on inserted.cm=cm2.cm where st.ref=inserted.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE UPDATE SA SET sa.stock=sa.stock+case when inserted.cm<50 then inserted.qtt else -inserted.qtt end, sa.qttacin=case when inserted.cm<50 then inserted.QTT+sa.qttacin else sa.qttacin end from INSERTED where sa.ref=inserted.ref and sa.armazem=inserted.armazem and EXISTS(select * from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and inserted.datalc >= @@STICLASTDATE UPDATE SX SET sx.stock=sx.stock+case when inserted.cm<50 then inserted.qtt else -inserted.qtt end from INSERTED where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and EXISTS(select * from sx where sx.ref=inserted.ref and inserted.armazem=sx.armazem and sx.cor=inserted.cor and sx.tam =inserted.tam) and inserted.datalc >= @@STICLASTDATE END ELSE BEGIN UPDATE ST SET st.stock = st.stock +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ) end, st.qttacout = st.qttacout +case when (SELECT SUM(case when inserted.cm<50 then 0 else inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then 0 else inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ) end, st.qttacin = st.qttacin +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ) end from inserted WHERE st.ref IN (SELECT ref FROM inserted) and inserted.datalc >= @@STICLASTDATE UPDATE ST SET  st.uintr=case when inserted.datalc>=st.uintr  and inserted.cm<50 then inserted.datalc else st.uintr end,  st.pcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.vu else st.pcult end,  st.usaid=case when inserted.datalc>=st.usaid  and inserted.cm>50 then inserted.datalc else st.usaid end,  st.pvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50   and inserted.qtt>0 and inserted.origem='FT' then inserted.vu else st.pvultimo end,  st.epcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.evu else st.epcult end,  st.epvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50  and inserted.qtt>0  and inserted.origem='FT' then inserted.evu else st.epvultimo end,  st.udata=inserted.datalc  from INSERTED inner join cm2 on inserted.cm=cm2.cm where st.ref=inserted.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE UPDATE SA SET sa.stock = sa.stock +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem  and inserted.datalc >= @@STICLASTDATE ) end, sa.qttacin = sa.qttacin +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem  and inserted.datalc >= @@STICLASTDATE ) end from inserted inner join sa on inserted.ref=sa.ref and inserted.armazem=sa.armazem where inserted.datalc >= @@STICLASTDATE UPDATE SX SET sx.stock = sx.stock +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tam  and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tam  and inserted.datalc >= @@STICLASTDATE ) end from inserted inner join sx on inserted.ref=sx.ref and inserted.cor=sx.cor and inserted.tam=sx.tam and inserted.armazem=sx.armazem  where inserted.datalc >= @@STICLASTDATE END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sl_delete ON SL WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONDECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTDECLARE @@STICLASTDATE datetimeSELECT @@STICLASTDATE = (select case when exists(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc)  then convert(datetime,(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc),121) else convert(datetime,'19000101',121) end) DECLARE @Cinfo VARBINARY(128) SELECT @Cinfo = Context_Info() IF @Cinfo = 0x88888 BEGIN RETURN END IF @@MROW = 1 BEGIN IF exists(select * from deleted where deleted.datalc < @@STICLASTDATE)RETURN END IF (select count(*) from deleted where deleted.cm<50)>0 BEGINUPDATE ST SET st.pcpond=case when st.stock-deleted.qtt>0  and deleted.cm<50 and cm2.mudapcpond=1 then (st.pcpond*(case when st.stock>0 then st.stock else 0 end)-deleted.vu*deleted.qtt)/((case when st.stock>0 then st.stock else 0 end)-deleted.qtt) else st.pcpond end,st.epcpond=case when st.stock-deleted.qtt>0  and deleted.cm<50 and cm2.mudapcpond=1 then (st.epcpond*(case when st.stock>0 then st.stock else 0 end)-deleted.evu*deleted.qtt)/((case when st.stock>0 then st.stock else 0 end)-deleted.qtt) else st.epcpond end from deleted inner join cm2 on deleted.cm=cm2.cm where st.ref=deleted.ref and deleted.trfa=0 and deleted.datalc >= @@STICLASTDATE 			-- apagar tabela temporaria (se existir)
			IF OBJECT_ID('tempdb..##cr_lastsl') IS NOT NULL
			DROP TABLE ##cr_lastsl

			-- criar tabela temporario com ultimo mov stock
			Select sl_last.sl_ultimo, sl.slstamp as real, sl.evu, sl.vu
			, sl_last.ref, sl.ousrdata
			--, sl.ousrhora
			-- corrigir registos com ousrhora desformatado para não dar erro a converter ---[      ]- (PMF) - 20.09.2022 -
			, case when len(rtrim(ltrim(sl.ousrhora))) = 5	then 

				case when substring(sl.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0')   
			  and substring(sl.ousrhora,4,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl.ousrhora,5,1) in ('1','2','3','4','5','6','7','8','9','0')
				then sl.ousrhora +':00' else 
					case when substring(sl.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
					 and substring(sl.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0') 
					then substring(sl.ousrhora,1,1)+substring(sl.ousrhora,2,1)+':00:00'
					else '00:00:00' end
				end 

				when len(rtrim(ltrim(sl.ousrhora))) = 8 then 

				case when substring(sl.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0')   
			  and substring(sl.ousrhora,4,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl.ousrhora,5,1) in ('1','2','3','4','5','6','7','8','9','0')
			  and substring(sl.ousrhora,7,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl.ousrhora,8,1) in ('1','2','3','4','5','6','7','8','9','0') 
				then sl.ousrhora 
				else 
					case when substring(sl.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
					 and substring(sl.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0') 
					then substring(sl.ousrhora,1,1)+substring(sl.ousrhora,2,1)+':00:00'
					else '00:00:00' end
				end 
				else
				'00:00:00'
				end as ousrhora	
							
			, sl_last.ousrdata as last_data
			--, sl_last.ousrhora as last_hora
			-- corrigir registos com ousrhora desformatado para não dar erro a converter ---[      ]- (PMF) - 20.09.2022 -
			, case when len(rtrim(ltrim(sl_last.ousrhora))) = 5	then 

				case when substring(sl_last.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl_last.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0')   
			  and substring(sl_last.ousrhora,4,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl_last.ousrhora,5,1) in ('1','2','3','4','5','6','7','8','9','0')
				then sl_last.ousrhora +':00' else 
					case when substring(sl_last.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
					 and substring(sl_last.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0') 
					then substring(sl_last.ousrhora,1,1)+substring(sl_last.ousrhora,2,1)+':00:00'
					else '00:00:00' end
				end 

				when len(rtrim(ltrim(sl_last.ousrhora))) = 8 then 

				case when substring(sl_last.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl_last.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0')   
			  and substring(sl_last.ousrhora,4,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl_last.ousrhora,5,1) in ('1','2','3','4','5','6','7','8','9','0')
			  and substring(sl_last.ousrhora,7,1) in ('1','2','3','4','5','6','7','8','9','0') 
			  and substring(sl_last.ousrhora,8,1) in ('1','2','3','4','5','6','7','8','9','0') 
				then sl_last.ousrhora 
				else 
					case when substring(sl_last.ousrhora,1,1) in ('1','2','3','4','5','6','7','8','9','0') 
					 and substring(sl_last.ousrhora,2,1) in ('1','2','3','4','5','6','7','8','9','0') 
					then substring(sl_last.ousrhora,1,1)+substring(sl_last.ousrhora,2,1)+':00:00'
					else '00:00:00' end
				end 
				else
				'00:00:00'
				end as last_hora			

			into ##cr_lastsl
			from (
			Select
			-- obter stamp do ultimo sl
			(select top 1 sl.slstamp
			from sl (nolock)
			inner join cm2 on cm2.cm = sl.cm and cm2.mudapcult = 1
			where sl.ref = deleted.ref and sl.cm <50 and sl.trfa=0
			order by sl.ousrdata desc, sl.ousrhora desc
			) as sl_ultimo
			, deleted.ref, deleted.ousrdata,  deleted.ousrhora
			from deleted
			) sl_last
			left join sl (nolock) on sl.slstamp = sl_last.sl_ultimo

			-- apaga da temporaria com data superior aos registos que está a apagar
			delete from ##cr_lastsl where ousrdata + ousrhora > last_data + last_hora

			-- update dos preços na tabela st
			-- Select * from ##cr_lastsl
			IF (select count(*) from ##cr_lastsl)>0
			begin
			update st set
			-- st.epcusto = isnull(lastsl.evu, 0),
			-- st.pcusto = isnull(lastsl.vu, 0),
			st.epcult = isnull(lastsl.evu, 0),
			st.pcult = isnull(lastsl.evu, 0)
			from st
			inner join ##cr_lastsl lastsl on lastsl.ref = st.ref
			end
			
			-- update dos preços na tabela se
			IF (select count(*) from ##cr_lastsl)>0
			begin
			update se set
			se.epcult = isnull(lastsl.evu, 0),
			se.pcult = isnull(lastsl.evu, 0)
			from se
			inner join ##cr_lastsl lastsl on lastsl.ref = se.ref
			end			-- apagar tabela temporaria (se existir)
			IF OBJECT_ID('tempdb..##cr_lastsl') IS NOT NULL
			DROP TABLE ##cr_lastslENDIF @@MROW = 1BEGIN UPDATE ST SET st.stock=st.stock-case when deleted.cm<50 then deleted.qtt else -deleted.qtt end,st.qttacin=case when deleted.cm<50 then -deleted.QTT+st.qttacin else st.qttacin end, st.qttacout=case when deleted.cm>50 then -deleted.QTT+st.qttacout else st.qttacout end from deleted where st.ref=deleted.ref and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE UPDATE SA SET sa.stock=sa.stock+case when deleted.cm<50 then -deleted.qtt else deleted.qtt end, sa.qttacin=case when deleted.cm<50 then -deleted.QTT+sa.qttacin else sa.qttacin end from deleted where sa.ref=deleted.ref and sa.armazem=deleted.armazem  and deleted.datalc >= @@STICLASTDATE UPDATE SX SET sx.stock=sx.stock+case when deleted.cm<50 then -deleted.qtt else deleted.qtt end from deleted where sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam  and deleted.datalc >= @@STICLASTDATE END ELSE BEGIN UPDATE ST SET st.stock = st.stock -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) end, st.qttacin = st.qttacin -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) end, st.qttacout = st.qttacout -case when (SELECT SUM(case when deleted.cm<50 then 0 else deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then 0 else deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) end  from deleted WHERE st.ref IN (SELECT ref FROM deleted) and deleted.datalc >= @@STICLASTDATE UPDATE SA SET sa.stock = sa.stock -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) end, sa.qttacin = sa.qttacin -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) end from deleted inner join sa on deleted.ref=sa.ref and deleted.armazem=sa.armazem where deleted.datalc >= @@STICLASTDATE UPDATE SX SET sx.stock = sx.stock -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam  and deleted.datalc >= @@STICLASTDATE ) end from deleted inner join sx on deleted.ref=sx.ref and deleted.cor=sx.cor and deleted.tam=sx.tam and deleted.armazem=sx.armazem  where deleted.datalc >= @@STICLASTDATE END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_anexos_update ON Anexos WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON	    DECLARE @allowedTables NVARCHAR(MAX) = 'FT,BO,FO,RD,RE,PD,OW';
		DECLARE @oritable NVARCHAR(80), 
		        @anexosstamp NVARCHAR(25), 
		        @recstamp NVARCHAR(25),
		        @original BIT,  
		        @tableToUpdate NVARCHAR(80),
		        @stampColumn NVARCHAR(80),
		        @sql NVARCHAR(MAX),
		        @updateFlag BIT,
		        @requiredValue BIT,
				@anuladoFieldTable NVARCHAR(80);

		-- Verifica se existem registros válidos
		IF EXISTS (
		    SELECT 1 
		    FROM inserted i
		    JOIN deleted d ON i.anexosstamp = d.anexosstamp
		    WHERE CHARINDEX(',' + UPPER(i.oritable) + ',', ',' + @allowedTables + ',') > 0 
		      AND i.original <> d.original
		)
		BEGIN
		    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
		    SELECT UPPER(oritable), anexosstamp, original, recstamp
		    FROM inserted
		    WHERE CHARINDEX(',' + UPPER(oritable) + ',', ',' + @allowedTables + ',') > 0;

		    OPEN cur;

		    FETCH NEXT FROM cur INTO @oritable, @anexosstamp, @original, @recstamp;

		    WHILE @@FETCH_STATUS = 0
		    BEGIN
					
		SET @anuladoFieldTable = CASE
									WHEN @oritable = 'BO' THEN 'bo2'
									WHEN @oritable = 'FO' THEN 'fo2'
									WHEN @oritable = 'FT' OR @oritable = 'RD' OR @oritable = 'RE' THEN @oritable
									ELSE ''
								END;

		-- Define a tabela e coluna stamp com base em `oritable`
		SET @tableToUpdate = CASE
		                        WHEN @oritable = 'FT' THEN 'ft3'
		                        WHEN @oritable = 'BO' THEN 'bo3'
		                        WHEN @oritable = 'FO' THEN 'fo2'
		                        ELSE @oritable
		                     END;

		SET @stampColumn = CASE
		                        WHEN @oritable = 'FT' THEN 'ft3stamp'
		                        WHEN @oritable = 'BO' THEN 'bo3stamp'
		                        WHEN @oritable = 'FO' THEN 'fo2stamp'
		                        ELSE @oritable + 'stamp'
		                   END;

		-- Inicializa a flag de atualização como 0
		SET @updateFlag = 0;

		-- Verificações específicas por tabela
		IF @oritable = 'BO'
		BEGIN
		    IF EXISTS (SELECT 1 FROM bo INNER JOIN ts ON bo.ndos = ts.ndos
		              WHERE ts.tiposaft NOT IN ('', '--') AND bo.bostamp = @recstamp)
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'FT'
		BEGIN
		    IF EXISTS (SELECT 1 FROM ft INNER JOIN td ON ft.ndoc = td.ndoc
		              WHERE (CASE WHEN ((td.docsimport = 0 AND td.docsext = 0) OR td.docsext = 1) THEN 0 ELSE td.excluisaft END = 0)
		              AND td.tiposaft NOT IN ('', '--') AND ft.ftstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'OW'
		BEGIN
		    IF EXISTS (SELECT 1 FROM ow WHERE (ow.ozstamp = '' AND ow.prstamp = '' AND ow.frstamp = '' AND ow.vrstamp = '' AND ow.postamp = '' AND ow.restamp = '')
		              AND ow.owstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'RD'
		BEGIN
		    IF EXISTS (SELECT 1 FROM rd INNER JOIN tsrd ON rd.ndoc = tsrd.ndoc
		              WHERE tsrd.tiposaft NOT IN ('', '--') AND rd.rdstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'RE'
		BEGIN
		    IF EXISTS (SELECT 1 FROM re INNER JOIN tsre ON re.ndoc = tsre.ndoc
		              WHERE tsre.movinc = 0 AND re.restamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE
		BEGIN
		    -- Para as demais tabelas, atualiza sem condições adicionais
		    SET @updateFlag = 1;
		END
				
		        -- Se a flag de atualização for 1, continua com a lógica para atualizar o campo ArquivadoDigital
		        IF @updateFlag = 1
		        BEGIN
		            -- Define o valor necessário para ArquivadoDigital
		            BEGIN
					    SET @sql = '
					        SELECT @result = CASE 
					            WHEN EXISTS (
					                SELECT 1 FROM anexos ';

					    -- Conditionally add the LEFT JOIN if the table is provided
					    IF @anuladoFieldTable <> ''
					    BEGIN
					        SET @sql = @sql + '
					                LEFT JOIN ' + QUOTENAME(@anuladoFieldTable) + ' 
					                ON ' + QUOTENAME(@anuladoFieldTable) + '.' + QUOTENAME(@anuladoFieldTable + 'STAMP') + ' = @recstamp ';
					    END

					    SET @sql = @sql + '
					                WHERE oritable = @oritable 
					                  AND recstamp = @recstamp 
					                  AND original = 1 ';

					    -- Conditionally add the anulado condition if the table is provided
					    IF @anuladoFieldTable <> ''
					    BEGIN
					        SET @sql = @sql + '
					                  AND (' + QUOTENAME(@anuladoFieldTable) + '.anulado <> 1 
					                       OR (' + QUOTENAME(@anuladoFieldTable) + '.anulado = 1 AND eanulado = 1))';
					    END

					    SET @sql = @sql + '
					            ) THEN 1
					            ELSE 0
					        END;';
					END

						EXEC sp_executesql @sql, 
							N'@recstamp NVARCHAR(25), @oritable NVARCHAR(80), @anexosstamp NVARCHAR(25), @result BIT OUTPUT', 
							@recstamp = @recstamp, 
							@oritable = @oritable, 
							@anexosstamp = @anexosstamp, 
							@result = @requiredValue OUTPUT;

		            -- Cria o comando SQL dinâmico para atualização
		            SET @sql = 'IF EXISTS (SELECT 1 FROM ' + QUOTENAME(@tableToUpdate) +
		                       ' WHERE ' + QUOTENAME(@stampColumn) + ' = @recstamp AND ArquivadoDigital <> @requiredValue) ' +
		                       'BEGIN ' +
		                           'UPDATE ' + QUOTENAME(@tableToUpdate) +
		                           ' SET ArquivadoDigital = @requiredValue ' +
		                           'WHERE ' + QUOTENAME(@stampColumn) + ' = @recstamp; ' +
		                       'END;';

		            -- Executa o SQL dinâmico para cada registro individualmente
		            EXEC sp_executesql @sql, 
		                N'@recstamp NVARCHAR(25), @requiredValue BIT', 
		                @recstamp = @recstamp, 
		                @requiredValue = @requiredValue;
		        END

		        FETCH NEXT FROM cur INTO @oritable, @anexosstamp, @original , @recstamp;
		    END

		    CLOSE cur;
		    DEALLOCATE cur;
		ENDif update(grupo) or update(fiscrel) or update(flen)
begin
    update anexos
    set marcadoenviar = 1
    from anexos a
    inner join inserted i on a.anexosstamp = i.anexosstamp
    inner join deleted d on d.anexosstamp = i.anexosstamp
    where a.marcadoenviar <> 1 
	  and not (
	        isnull((select top 1 valor 
	                from para1 
	                where descricao = 'ge_transferfiscrelonly'), '') = 'S'
	        and i.fiscrel = 0
	      )        
      and (
            isnull(i.grupo, '') <> isnull(d.grupo, '')
            or isnull(i.fiscrel, 0) <> isnull(d.fiscrel, 0)
            or isnull(i.flen, -1) <> isnull(d.flen, -1)
        );
        
end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bo2_update ON BO2 WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONIF exists(select 1 from inserted inner join deleted on deleted.bo2stamp=inserted.bo2stamp where deleted.anulado=0 and inserted.anulado=1)BEGIN if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1) BEGIN DECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTIF @@MROW = 1BEGIN if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.rescli=1 and bi.fechada=0 and inserted.anulado=1) BEGINUPDATE ST SET st.qttcli=st.qttcli-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and st.ref=bi.ref and bi.rescli=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 UPDATE SA SET sa.rescli =sa.rescli-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and sa.ref=bi.ref and sa.armazem=bi.armazem and bi.rescli=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 ENDif exists (select bi.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.cativo=1 and bi.fechada=0 and inserted.anulado=1) BEGINUPDATE ST SET st.qttcat=st.qttcat-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and st.ref=bi.ref and bi.cativo=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 UPDATE SA SET sa.rescat =sa.rescat-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and sa.ref=bi.ref and sa.armazem=bi.armazem and bi.cativo=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resfor=1 and bi.fechada=0 and inserted.anulado=1) BEGINUPDATE ST SET st.qttfor=st.qttfor-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and st.ref=bi.ref and bi.resfor=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 UPDATE SA SET sa.resfor =sa.resfor-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and sa.ref=bi.ref and sa.armazem=bi.armazem and bi.resfor=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resusr=1 and bi.fechada=0 and inserted.anulado=1) UPDATE ST SET st.usrqtt=st.usrqtt-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and st.ref=bi.ref and bi.resusr=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resrec=1 and bi.fechada=0 and inserted.anulado=1) BEGINUPDATE ST SET st.qttrec=st.qttrec-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and st.ref=bi.ref and bi.resrec=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 UPDATE SA SET sa.qttrec =sa.qttrec-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and sa.ref=bi.ref and sa.armazem=bi.armazem and bi.resrec=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.rescli=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.rescli=sx.rescli-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and sx.ref=bi.ref and sx.armazem=bi.armazem and sx.cor=bi.cor and sx.tam=bi.tam and bi.rescli=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.cativo=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.rescat=sx.rescat-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and sx.ref=bi.ref and sx.armazem=bi.armazem and sx.cor=bi.cor and sx.tam=bi.tam and bi.cativo=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resfor=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.resfor =sx.resfor-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and sx.ref=bi.ref and sx.armazem=bi.armazem and sx.cor=bi.cor and sx.tam=bi.tam and bi.resfor=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resrec=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.qttrec=sx.qttrec-(case when (bi.qtt-bi.qtt2)<0 then 0 else (bi.qtt-bi.qtt2) end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and sx.ref=bi.ref and sx.armazem=bi.armazem and sx.cor=bi.cor and sx.tam=bi.tam and bi.resrec=1 and bi.fechada=0 and bi.emconf=0 and inserted.anulado=1 END ELSE BEGIN if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resrec=1 and bi.fechada=0 and inserted.anulado=1)  BEGINUPDATE SA SET sa.qttrec =sa.qttrec-case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazem and bi.fechada=0 and inserted.anulado=1 and bi.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazem and bi.fechada=0 and bi.resrec=1 and inserted.anulado=1) end WHERE sa.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)UPDATE st SET st.qttrec =st.qttrec-case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and inserted.anulado=1 and bi.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and bi.resrec=1 and inserted.anulado=1) end WHERE st.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.rescli=1 and bi.fechada=0 and inserted.anulado=1)  BEGINUPDATE SA SET sa.rescli = sa.rescli -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazem and bi.fechada=0 and inserted.anulado=1 and bi.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazemand bi.fechada=0 and bi.rescli=1 and inserted.anulado=1) end WHERE sa.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)UPDATE st SET st.qttcli = st.qttcli -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and inserted.anulado=1 and bi.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and bi.rescli=1 and inserted.anulado=1) end WHERE st.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.cativo=1 and bi.fechada=0 and inserted.anulado=1) BEGINUPDATE SA SET sa.rescat = sa.rescat -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazem and bi.fechada=0 and inserted.anulado=1 and bi.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazemand bi.fechada=0 and bi.cativo=1 and inserted.anulado=1) end WHERE sa.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)UPDATE st SET st.qttcat = st.qttcat -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and inserted.anulado=1 and bi.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and bi.cativo=1 and inserted.anulado=1) end WHERE st.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resfor=1 and bi.fechada=0 and inserted.anulado=1)  BEGINUPDATE SA SET sa.resfor = sa.resfor -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazem and bi.fechada=0 and inserted.anulado=1 and bi.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=sa.ref and bi.armazem=sa.armazemand bi.fechada=0 and bi.resfor=1 and inserted.anulado=1) end WHERE sa.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)UPDATE st SET st.qttfor = st.qttfor -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and inserted.anulado=1 and bi.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and bi.resfor=1 and inserted.anulado=1) end WHERE st.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)ENDif exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resusr=1 and bi.fechada=0 and inserted.anulado=1)  UPDATE st SET st.usrqtt = st.usrqtt -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and inserted.anulado=1 and bi.resusr=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and bi.ref=st.ref and bi.fechada=0 and bi.resusr=1 and inserted.anulado=1) end WHERE st.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)if exists (select inserted.marcada from inserted inner join bi(nolock) on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resrec=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.qttrec =sx.qttrec-case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tam and bi.fechada=0 and inserted.anulado=1 and bi.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tamand bi.fechada=0 and bi.resrec=1 and inserted.anulado=1) end WHERE sx.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)if exists (select inserted.marcada from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.rescli=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.rescli = sx.rescli -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tam and bi.fechada=0 and inserted.anulado=1 and bi.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tamand bi.fechada=0 and bi.rescli=1 and inserted.anulado=1) end WHERE sx.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)if exists (select inserted.marcada from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.cativo=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.rescat = sx.rescat -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tam and bi.fechada=0 and inserted.anulado=1 and bi.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tamand bi.fechada=0 and bi.cativo=1 and inserted.anulado=1) end WHERE sx.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)if exists (select inserted.marcada from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and bi.resfor=1 and bi.fechada=0 and bi.cor<>'' and bi.tam<>'' and inserted.anulado=1)  UPDATE Sx SET sx.resfor = sx.resfor -case when (SELECT SUM(qtt-qtt2) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tam and bi.fechada=0 and inserted.anulado=1 and bi.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp WHERE bi.ref<>'' and (bi.cor<>'' or bi.tam<>'') and bi.ref=sx.ref and bi.armazem=sx.armazem and bi.cor=sx.cor and bi.tam=sx.tamand bi.fechada=0 and bi.resfor=1 and inserted.anulado=1) end WHERE sx.ref IN (SELECT bi.ref from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.emconf=0 and inserted.anulado=1)END if exists(select top 1 sl.slstamp from sl(nolock) inner join bi(nolock) on bi.ref<>'' and sl.bistamp=bi.bistamp and bi.bistamp<>'' and bi.emconf=0 inner join inserted on inserted.bo2stamp = bi.bostamp and inserted.anulado=1) DELETE FROM SL from bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where bi.ref<>'' and sl.bistamp=bi.bistamp and bi.bistamp<>'' and bi.emconf=0 and inserted.anulado=1if exists(select top 1 bi.bistamp from bi(nolock) inner join bi delBI (nolock) on delBI.obistamp<>'' and delBI.obistamp=bi.bistamp and delBI.emconf=0 inner join bo(nolock) on delBI.bostamp=bo.bostamp inner join inserted on delBI.bostamp = inserted.bo2stamp and inserted.anulado=1) BEGIN UPDATE BI SET bi.fno = 0,bi.adoc = ' ',bi.nmdoc = ' ',bi.ndoc = 0, bi.qtt2 = bi.qtt2-(SELECT SUM(delBI1.qtt) FROM bi delBI1 inner join inserted on inserted.bo2stamp = delBI1.bostamp inner join bo(nolock) on delBI1.bostamp=bo.bostamp where delBI1.obistamp<>'' and delBI1.obistamp=bi.bistamp and delBI1.emconf=0 and inserted.anulado=1) from bi inner join bi delBI on delBI.obistamp=bi.bistamp inner join inserted on inserted.bo2stamp = delBI.bostamp inner join bo(nolock) on delBI.bostamp=bo.bostamp where delBI.obistamp<>'' and delBI.obistamp=bi.bistamp and delBI.emconf=0 and inserted.anulado=1UPDATE BI SET bi.partes2 = bi.partes2-(SELECT SUM(delBI1.partes) FROM bi delBI1 inner join inserted on inserted.bo2stamp = delBI1.bostamp inner join bo(nolock) on delBI1.bostamp=bo.bostamp where delBI1.obistamp<>'' and delBI1.obistamp=bi.bistamp and delBI1.emconf=0 and inserted.anulado=1) from bi inner join bi delBI on delBI.obistamp=bi.bistamp inner join inserted on inserted.bo2stamp = delBI.bostamp inner join ts(nolock) on ts.ndos = bi.ndos where delBI.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and delBI.emconf=0 and inserted.anulado=1END if exists(select top 1 biprecat.bistampfor from biprecat(nolock) inner join bi(nolock) on biprecat.bistampfor=bi.bistamp inner join inserted on bi.bostamp = inserted.bo2stamp inner join ts(nolock) on bi.ndos=ts.ndos inner join ts2(nolock) on ts2.ts2stamp=ts.tsstamp where bi.resfor=1 and ts2.precativo=1 and inserted.anulado=1) DELETE FROM BIPRECAT FROM bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where biprecat.bistampfor=bi.bistamp and inserted.anulado=1if exists(select top 1 biprecat.bistampcli from biprecat(nolock) inner join bi(nolock) on biprecat.bistampcli=bi.bistamp inner join inserted on inserted.bo2stamp = bi.bostamp inner join ts(nolock) on bi.ndos=ts.ndos where bi.rescli=1 and ts.cativo=1 and inserted.anulado=1) DELETE FROM BIPRECAT FROM bi(nolock) inner join inserted on inserted.bo2stamp = bi.bostamp where biprecat.bistampcli=bi.bistamp and inserted.anulado=1 END END UPDATE BO3 SET BO3.arquivadodigital = 0  FROM inserted  INNER JOIN deleted ON deleted.BO2stamp=inserted.BO2stamp  INNER JOIN BO2 ON BO2.BO2stamp=inserted.BO2stamp  WHERE inserted.anulado=1 AND deleted.anulado=0 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
 CREATE PROCEDURE [dbo].[usp_stinvclc] @msel nvarchar(max)
, @ordenacao nvarchar(max)
, @mpreco int
, @mwhat int 
, @st_pcparm bit 
, @st_pond bit
, @mlotes bit 
, @st_lotecusto bit
, @mgroup nvarchar(max)
, @marmazem int		
, @cindex nvarchar(max)	
, @lInvATfiltraArm bit	
, @tmpx nvarchar(100) = null	
, @tmpxapoio nvarchar(100) = null							WITH ENCRYPTION, EXECUTE AS OWNERAS  SET NOCOUNT ON; 	BEGIN TRANSACTION 
	BEGIN TRY
		declare @TSqlCursor nvarchar(max) = N''
		declare @colunasCursor nvarchar(max) = N''
		declare @VariaveiscolunasCursor nvarchar(max) = N''
		declare @DecVariaveisCursor nvarchar(max) = N''
		declare @creIndex nvarchar(max) = N''					
		declare @mgroup2 nvarchar(max)
		declare @sqlDropTmpx nvarchar(100)
		declare @sqlDropTmpApoio nvarchar(100)
		declare @sqlTmp nvarchar(max)

		-- Controla preço custo ponderado com stk negativo  ---[243166]- (NB) - 09-12-2025 -
		declare @lbctrl_stpond_neg bit = (Select case isnull( (Select valor from para1 where descricao ='stinv_ctrl_pcpond'), 'N') 
			when 'S' then 1 else 0 end)
		set @lbctrl_stpond_neg = (select case when @st_pond =1 and @lbctrl_stpond_neg=1 then 1 else 0 end)
		
		if @tmpx IS NULL OR LTRIM(RTRIM(@tmpx)) = ''
			set @tmpx = 'tmpx_' + CAST(@@SPID AS NVARCHAR(10))
			
		if @tmpxapoio IS NULL OR LTRIM(RTRIM(@tmpxapoio)) = ''
			set @tmpxapoio = 'tmpxapoio_' + CAST(@@SPID AS NVARCHAR(10))
   
		if @tmpx LIKE '%[^a-zA-Z0-9_]%'
		begin
			RAISERROR ('Nome inválido de tabela: contém caracteres proibidos.', 16, 1);
			RETURN;
		end

		if @tmpxapoio LIKE '%[^a-zA-Z0-9_]%'
		begin
			RAISERROR ('Nome inválido de tabela: contém caracteres proibidos.', 16, 1);
			RETURN;
		end
		
		if @st_pcparm = 0 
			set @mgroup2 =REPLACE(REPLACE(@mgroup,'@armazem',1),'armazem',1)
		else
			set @mgroup2 =@mgroup
			
		-- Drop da primeira tabela
		set @sqlDropTmpx = N'if OBJECT_ID(''dbo.' + @tmpx + N''') IS NOT NULL DROP TABLE dbo.' + @tmpx + N';';
		EXEC sp_executesql @sqlDropTmpx;

		-- Drop da segunda tabela
		set @sqlDropTmpApoio = N'if OBJECT_ID(''dbo.' + @tmpxapoio + N''') IS NOT NULL DROP TABLE dbo.' + @tmpxapoio + N';';
		EXEC sp_executesql @sqlDropTmpApoio;

		-- cria 2º tabela de apoio com dados
		set @sqlTmp = N'Select * Into ' + QUOTENAME(@tmpx) + N' from (' + @msel + N') t';
		EXEC sp_executesql @sqlTmp;

		-- cria 1º tabela de dados (vazia)
		set @sqlTmp = '
			SELECT TOP 1 * INTO '+ QUOTENAME(@tmpxapoio) +' FROM '+ QUOTENAME(@tmpx) + ';
			DELETE FROM ' + QUOTENAME(@tmpxapoio) + ';
			';
		EXEC sp_executesql @sqlTmp;

		set @creIndex = 'CREATE INDEX idx_' + CAST(@@SPID AS NVARCHAR(10)) + ' ON dbo.' + @tmpxapoio + ' (' + @cindex + ')';
		EXEC sp_executesql @creIndex  
		
		-- obtem 3 colunas: nomes das colunas, variaveis com nome igual aos campos, declare das variaveis
		-- exemplo coluna 1: Id, Nome, DataCriacao, Valor
		-- exemplo coluna 2: @Id, @Nome, @DataCriacao, @Valor
		-- exemplo coluna 3: DECLARE @Id int .... etc
		
		select @colunasCursor = @colunasCursor 
					+ CASE WHEN @colunasCursor <> N'' THEN N' ,' else N'' end 
					+ rtrim(convert(varchar(20),col.[name])) 
			, @VariaveiscolunasCursor = @VariaveiscolunasCursor 
					+ CASE WHEN @VariaveiscolunasCursor <> N'' THEN N' ,' else N'' end 
					+ rtrim('@'+convert(varchar(20),col.[name]))
			, @DecVariaveisCursor =  @DecVariaveisCursor 
					+ CASE WHEN @VariaveiscolunasCursor <> N'' THEN char(13) else N'' end 
					+'	declare '+  rtrim('@'+convert(varchar(20),col.[name])) +  rtrim(' '+convert(varchar(20),ty.[name]))
					+ case when  ty.[name] = 'varchar' or ty.[name] = 'char' then ' (' + cast(col.max_length as varchar(20))+')'
						else ''
						end 
					+ case when  ty.[name] = 'numeric' then ' (' + cast(col.precision as varchar(20)) + ',' +  cast(col.scale as varchar(20)) +')'
						else ''
						end 
			from sys.columns col 
			inner join sys.types ty on ty.system_type_id =col.system_type_id and ty.is_user_defined =0
			where object_id = object_id('dbo.' + @tmpx)

		set @TsqlCursor = @DecVariaveisCursor
		set @TsqlCursor = @TsqlCursor + CHAR(13) + 
			'
			declare @mpreco int = '+ cast(@mpreco as varchar(10)) +' 
			declare @mwhat int = '+ cast(@mwhat as varchar(10)) +'  
			declare @st_pcparm bit = '+ cast(@st_pcparm as varchar(10)) +' 
			declare @st_pond bit = '+ cast(@st_pond as varchar(10)) +' 
			declare @marmazem int = '+ cast(@marmazem as varchar(10)) +'
			declare @st_lotecusto bit = '+ cast(@st_lotecusto as varchar(10))+'
			declare @lInvATfiltraArm bit = '+ cast(@lInvATfiltraArm as varchar(10)) +'
			declare @mpcpond numeric(19,6) = 0.000
			declare @mqtt numeric(19,6) = 0.000
			declare @inseretmpxapoio bit = 1
			declare @mpcpondOld numeric(19,6) = 0.000
			declare @mlotes bit = '+cast(@mlotes as varchar(10))+'
			declare @lbctrl_stpond_neg bit = '+ cast(@lbctrl_stpond_neg as varchar(10)) +'
			'

		-- declara cursor e pesquisa registos
		set @TsqlCursor = @TsqlCursor  + CHAR(13) +
			'
			DECLARE db_cursor CURSOR FAST_FORWARD FORWARD_ONLY  FOR   
				SELECT '+ @colunasCursor +' FROM dbo.' + QUOTENAME(@tmpx) + @ordenacao +' 
			OPEN  db_cursor
			FETCH NEXT FROM db_cursor INTO '+ @VariaveiscolunasCursor +'

			declare @mref varchar(20) = @ref
			declare @mvaiarm int = @armazem
			'

		if @mlotes = 1
			SET @TsqlCursor = @TsqlCursor + CHAR(13) +'		declare @mlote varchar(30) = @lote ' 
		
		set @TsqlCursor = @TsqlCursor + CHAR(13) +
			'
			-- ciclo todos registos
			WHILE @@FETCH_STATUS = 0 
			begin
				if @mpreco = 1
				begin
					set @mpcpond = @vu
				end
				else
				begin
					if (@mwhat<>2 Or @trfa = 0) 
					begin
						if @lValor =0
						begin
							if @CM<>0 and @MUDAPCPOND = 1 and ( @trfa = 0 Or @st_pcparm = 1) and @qtt+@mqtt<>0 and @CM<50
							begin
								if @qtt>0 and @mqtt<0 and (@qtt+@mqtt)>0
									set @mpcpond =@vu
								else
									if @mqtt>0 or @qtt+@mqtt>0
										set @mpcpond =(@mpcpond*@mqtt+@qtt*@vu) /(@qtt+@mqtt)
													
								if @mpcpond <0 or @mpcpond > 999999999999
									set @mpcpond = @mpcpondold
									
								-- inventário de Stock à data com pcponderado e stk negativo (controlo erro) ---[243166]- (NB) - 09-12-2025 -
								if @lbctrl_stpond_neg =1
									if @qtt + @mqtt < 0
										set @mpcpond = @mpcpondold
							end 
						end
						else
						begin 
							if @qtt+@mqtt <>0
							begin
								if @CM <50
									set @mpcpond =(@mpcpond*@mqtt+@vu) /(@qtt+@mqtt)
								if @mpcpond<0 or @mpcpond > 999999999999
									set @mpcpond = @mpcpondold

								-- inventário de Stock à data com pcponderado e stk negativo (controlo erro) ---[243166]- (NB) - 09-12-2025 -
								if @lbctrl_stpond_neg =1
									if @qtt + @mqtt < 0
										set @mpcpond = @mpcpondold
							end
						end 

						if @trfa=0 Or @st_pcparm = 1
						begin
							if @st_pond =1
								set @mqtt =@mqtt +case when @CM<50 then @qtt else -@qtt end 
							else
								set @mqtt =@mqtt +case when @CM<50 then @qtt else 0 end 
							end
						end  
						else 
						begin 
							if @lInvATfiltraArm=1 AND @mwhat = 2 AND @CM<50 AND @trfa = 1 AND @lvalor = 0
								set @mpcpond = @pcpond
						end 
					end 		
			
					-- guarda preço anterior
					set @mpcpondold =@mpcpond
			
					if @marmazem = @armazem or @marmazem = 0
					begin
						declare @existe as bit
						select @existe = CAST(
							ISNULL( (SELECT 1 WHERE NOT EXISTS (
								SELECT 1 FROM ' + QUOTENAME(@tmpxapoio) + ' WHERE ' + @mgroup + '))
								, 0) AS BIT
							);
					
						if @existe = 1
						begin 
							insert into ' + QUOTENAME(@tmpxapoio) + ' ('+@colunasCursor+')
								values (' + @VariaveiscolunasCursor + ')
							
							update ' + QUOTENAME(@tmpxapoio) + '
								set datalc =@datalc
									, qtt = case when @CM<50 then @qtt else @qtt*-1 end 
								where ' + @mgroup + '	

							update ' + QUOTENAME(@tmpxapoio) + '
								set pcpond = @mpcpond												
								where ' + @mgroup2 + '
						end
						else 
						begin
							update ' + QUOTENAME(@tmpxapoio) + '
								set datalc =@datalc
									, qtt = qtt + case when @CM<50 then @qtt else @qtt*-1 end 
								where ' + @mgroup + '

							update ' + QUOTENAME(@tmpxapoio) + '
								set pcpond = @mpcpond												
								where ' + @mgroup2 + '												
						end				
					end
 
					FETCH NEXT FROM db_cursor INTO ' + @VariaveiscolunasCursor + '

					if not Upper(rtrim(ltrim(@mref))) = Upper(rtrim(ltrim(@ref))) 
			'
				
		if @mlotes = 1 AND ( @mpreco = 4 and @st_lotecusto = 0)
			set @TsqlCursor = @TsqlCursor + CHAR(13) +'	Or (@mlote<>@lote) ' 

		set @TsqlCursor = @TsqlCursor  + CHAR(13) + 
			'				
					or (@st_pcparm = 1 and @mwhat<>2 and  @mvaiarm<>@armazem) 
					begin 
						set @mpcpond = 0
						set @mqtt = 0
						set @mref = @ref
						set @mvaiarm = @armazem 
			'

		if @mlotes = 1
			set @TsqlCursor = @TsqlCursor + CHAR(13) +'			set @mlote=@lote ' 

		set @TsqlCursor = @TsqlCursor  + CHAR(13)+ 
			'
				end 
			end
			CLOSE db_cursor 
			DEALLOCATE db_cursor 
			'

		EXEC sp_executesql  @TsqlCursor
	END TRY
	BEGIN CATCH 
		DECLARE @ErrorNumber INT 
		DECLARE @ErrorLine INT 
		DECLARE @ErrorMessage NVARCHAR(4000) 
		DECLARE @ErrorSeverity INT 
		DECLARE @ErrorState INT 

		SELECT @ErrorNumber = ERROR_NUMBER()
			, @ErrorMessage = 'Erro ao calcular o inventário' + ERROR_MESSAGE()
			, @ErrorSeverity = ERROR_SEVERITY()
			, @ErrorState  = ERROR_STATE() 

		RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);

		if @@TRANCOUNT > 0  
			ROLLBACK TRANSACTION;  
	END CATCH;  
			  
	if @@TRANCOUNT > 0  
		COMMIT TRANSACTION;  	
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rl_insert ON rl WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONUPDATE CC SET
cc.debf=cc.debf+ISNULL((select case when sum(inserted.ecambio)=0 and Sum(inserted.escrec)-Sum(inserted.arred)>0 then Sum(inserted.escrec)-Sum(inserted.arred) else
case when Sum(inserted.cambio)<>0 and Sum(inserted.escrec)>0 then Sum(inserted.escrec) else 0 end end
from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.credf=cc.credf-ISNULL((select case when sum(inserted.ecambio)=0 and Sum(inserted.escrec)-Sum(inserted.arred)<0 then Sum(inserted.escrec)-Sum(inserted.arred) else
case when Sum(inserted.cambio)<>0 and Sum(inserted.escrec)<0 then Sum(inserted.escrec) else 0 end end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.debfm=cc.debfm+ISNULL((select case when sum(inserted.ecambio)<>0 and Sum(inserted.rec)>0 then sum(inserted.rec) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.credfm=cc.credfm-ISNULL((select case when sum(inserted.ecambio)<>0 and Sum(inserted.rec)<0 then sum(inserted.rec) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.edebf=cc.edebf+ISNULL((select case when sum(inserted.erec)>0 then sum(inserted.erec)-sum(inserted.earred) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.ecredf =cc.ecredf-ISNULL((select case when sum(inserted.erec)<0 then sum(inserted.erec)-sum(inserted.earred) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.ivacatreg =cc.ivacatreg +ISNULL((select sum(abs(inserted.ivacativado))  from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.eivacatreg=cc.eivacatreg +ISNULL((select sum(abs(inserted.eivacativado)) from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.mivacatreg =cc.mivacatreg +ISNULL((select sum(abs(inserted.mivacativado))  from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.virsreg=cc.virsreg+ISNULL((select sum(inserted.virs)
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.evirsreg=cc.evirsreg+ISNULL((select sum(inserted.evirs)
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)

UPDATE CC
set cc.ultdoc= case when tsre.movinc = 0 then 'Recibo nº '+convert(char(10),inserted.rno)+' de '+convert(char(10),inserted.rdata,104)
else 'Recibo Incobrável nº '+convert(char(10),inserted.rno)+' de '+convert(char(10),inserted.rdata,104) end
, cc.recibado=1,cc.recino=inserted.rno,cc.recian=datepart(yy,inserted.rdata)
, cc.recindoc =inserted.ndoc
, cc.valre =cc.valre +inserted.escrec - inserted.ivacativado
, cc.evalre =cc.evalre+inserted.erec - inserted.eivacativado
, cc.mvalre =cc.mvalre+inserted.rec - inserted.mivacativado
, cc.valch = ISNULL(( select case when ISNULL(sum(re.total),0) <> 0 then round(((sum(rl.escrec)*100/sum(re.total))/100)*(sum(re.chtotal)),2)
else 0 end
from re (nolock)
inner join rl (nolock) on rl.restamp = re.restamp
where rl.ccstamp = inserted.ccstamp and inserted.cheque = 1),0)
,  cc.evalch = ISNULL(( select case when ISNULL(sum(re.etotal),0) <> 0 then round(((sum(rl.erec)*100/sum(re.etotal))/100)*(sum(re.echtotal)),2)
else 0 end
from re (nolock)
inner join rl (nolock) on rl.restamp = re.restamp
where rl.ccstamp = inserted.ccstamp and rl.cheque = 1),0)
, cc.cbbno = re.cbbno
from inserted
inner join re on re.restamp=inserted.restamp
inner join tsre on tsre.ndoc = re.ndoc
where cc.ccstamp=inserted.ccstamp
and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc )
and re.anulado=0

UPDATE CC
set cc.irsdif =cc.irsdif+case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.virs/(1-(case when inserted.desconto<>0 then inserted.desconto else re.fin end/100))),0)-inserted.virs) end
, cc.eirsdif =cc.eirsdif+case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.evirs/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),2)-inserted.evirs) end
, cc.ivacatdif =cc.ivacatdif +case when inserted.desconto=100 or re.fin=100 then 0
else (round((inserted.ivacativado/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),0)-inserted.ivacativado) end
, cc.eivacatdif =cc.eivacatdif +case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.eivacativado/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),2)-inserted.eivacativado) end
, cc.mivacatdif =cc.mivacatdif +case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.mivacativado/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),2)-inserted.mivacativado) end
from inserted
inner join re on re.restamp=inserted.restamp
where cc.ccstamp=inserted.ccstamp
and inserted.process=1 and (inserted.desconto<>0 or re.fin<>0)

if (select count(*) 
from inserted 
inner join re (nolock) on re.restamp=inserted.restamp
inner join tsre (nolock) on inserted.ndoc=tsre.ndoc 
where tsre.intropag=1)>0
UPDATE RD 
set rd.integrado=1 
from inserted
inner join cc (nolock) on cc.ccstamp = inserted.ccstamp
inner join rd (nolock) on rd.rdstamp = cc.rdstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rl_delete ON rl WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONUPDATE CC SET
cc.debf=cc.debf-ISNULL((select case when sum(deleted.cambio)= 0 and Sum(deleted.escrec)>0 then sum(deleted.escrec)-sum(deleted.arred) else
case when Sum(deleted.cambio)<>0 and Sum(deleted.escrec)>0 then Sum(deleted.escrec) else 0 end end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.credf=cc.credf+ISNULL((select case when sum(deleted.cambio)= 0 and Sum(deleted.escrec)<0 then sum(deleted.escrec)-sum(deleted.arred) else
case when Sum(deleted.cambio)<>0 and Sum(deleted.escrec)<0 then Sum(deleted.escrec) else 0 end end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.debfm=cc.debfm-ISNULL((select case when sum(deleted.ecambio)<>0 and Sum(deleted.rec)>0 then sum(deleted.rec) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.credfm=cc.credfm+ISNULL((select case when sum(deleted.ecambio)<>0 and Sum(deleted.rec)<0 then sum(deleted.rec) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.edebf=cc.edebf-ISNULL((select case when sum(deleted.erec)>0 then sum(deleted.erec)-sum(deleted.earred) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.ecredf=cc.ecredf+ISNULL((select case when sum(deleted.erec)<0 then sum(deleted.erec)-sum(deleted.earred) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0)
, cc.ivacatreg =cc.ivacatreg -ISNULL((select sum(abs(deleted.ivacativado)) from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1), 0)
, cc.eivacatreg =cc.eivacatreg -ISNULL((select sum(abs(deleted.eivacativado)) from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1), 0)
, cc.mivacatreg =cc.mivacatreg -ISNULL((select sum(abs(deleted.mivacativado)) from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1), 0)
, cc.virsreg=cc.virsreg-ISNULL((select sum(deleted.virs)
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.evirsreg=cc.evirsreg-ISNULL((select sum(deleted.evirs)
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0)
from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1

UPDATE CC
set cc.ultdoc=''
, cc.recibado=0,cc.recino=0,cc.recian=0
, cc.recindoc=0
, cc.valre =cc.valre -deleted.escrec + deleted.ivacativado
, cc.evalre =cc.evalre -deleted.erec + deleted.eivacativado
, cc.mvalre =cc.mvalre -deleted.rec + deleted.mivacativado
, cc.valch = ISNULL(( select case when ISNULL(sum(re.total),0) <> 0 then round(((sum(rl.escrec)*100/sum(re.total))/100)*(sum(re.chtotal)),2)
else 0 end
from re	
inner join rl on rl.restamp = re.restamp
where rl.ccstamp = deleted.ccstamp and re.restamp <> deleted.restamp and deleted.cheque = 1),0)
, cc.evalch = isnull((
select case when ISNULL(sum(re.etotal),0)<> 0 then round(((sum(rl.erec)*100/sum(re.etotal))/100)*(sum(re.echtotal)),2)
else 0 end
from re
inner join rl on rl.restamp = re.restamp
where rl.ccstamp = deleted.ccstamp and re.restamp <> deleted.restamp and deleted.cheque = 1),0)
, cc.cbbno=0
from deleted
where cc.ccstamp =deleted.ccstamp

UPDATE CC 
set cc.irsdif =cc.irsdif-case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.virs/(1-(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),0)-deleted.virs) end
, cc.eirsdif =cc.eirsdif-case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.evirs/(1-(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),2)-deleted.evirs) end
, cc.ivacatdif =cc.ivacatdif -case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.ivacativado /(1 -(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),0)-deleted.ivacativado) end
, cc.eivacatdif =cc.eivacatdif -case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.eivacativado /(1 -(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),2)-deleted.eivacativado) end
, cc.mivacatdif = cc.mivacatdif -case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.mivacativado /(1 -(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),2)-deleted.mivacativado) end
from deleted
inner join re on re.restamp=deleted.restamp
where cc.ccstamp=deleted.ccstamp and deleted.process=1 and (deleted.desconto<>0 or re.fin<>0)if (select count(*) from deleted inner join re on re.restamp=deleted.restamp inner join tsre (nolock) on deleted.ndoc=tsre.ndoc where tsre.intropag=1)>0 UPDATE RD set rd.integrado=(case when (select count(*) from cc (nolock) inner join rd (nolock) on rd.rdstamp=cc.rdstamp where cc.ccstamp=deleted.ccstamp)=1 then 0 else 1 end) from deleted inner join cc (nolock) on cc.ccstamp = deleted.ccstamp inner join rd (nolock) on rd.rdstamp = cc.rdstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_boma_update ON boma WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (select count(*) from deleted inner join inserted on deleted.bistamp=inserted.bistamp where deleted.emconf=0 or inserted.emconf=0)>0 BEGIN if (select count(*) from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0)>0 BEGIN IF (select count(*) from inserted where inserted.trfarm=1)>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.noarm=inserted.ar2mazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.trfarm=1 and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0)>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnome,flno,flestab,flnmdoc,fladoc,fldata,obs,foorigem,situacao) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nome,inserted.no,inserted.estab, inserted.nmdos,inserted.obrano,inserted.dataobra, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end,'BO',inserted.situacao from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where not exists (select bomastamp from ma where ma.bomastamp=inserted.bomastamp) and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=1)>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=1,ma.retfor=0,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra,  ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=1 and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.indisponivel=1 from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 where inserted.ctrmastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0  and not inserted.ctrmastamp='' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.nome=ma2.nome,ma.no=ma2.no,ma.morada=ma2.morada,ma.local=ma2.local,ma.codpost=ma2.codpost,ma.zona=ma2.zona,ma.estab=ma2.estab,ma.vendnm=ma2.vendnm,ma.vendedor=ma2.vendedor,ma.ftorigem='BO',ma.instal=ma2.instal, ma.telefone=ma2.telefone,ma.contacto=ma2.contacto,ma.email=ma2.email, ma.ftnmdoc=inserted.nmdos,ma.ftfno=inserted.obrano,ma.ftfdata=inserted.dataobra,ma.bostampv=inserted.bostamp,ma.obs=case when inserted.obs='' then ma2.obs else substring(ma2.obs,1,datalength(ma2.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join ma on ma.mastamp=inserted.bomastamp inner join ma ma2 on ma2.mastamp=inserted.ctrmastamp where ma2.mastamp=inserted.ctrmastamp and ma.mastamp=inserted.bomastamp and ma2.no>0 and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='' and inserted.emconf=0 END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome=inserted.nome,ma.no=inserted.no,ma.bostampv=inserted.bostamp, ma.casa=0, ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.estab=bo.estab,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor,ma.ftorigem='BO',ma.instal=bo.dataobra, ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra, ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where inserted.mastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=1,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.nome=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then inserted.nome else ma.nome end,ma.no=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then inserted.no else ma.no end,ma.estab=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.estab else ma.estab end,ma.morada=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.morada else ma.morada end,ma.local=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.local else ma.local end,ma.codpost=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.codpost else ma.codpost end,ma.zona=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.zona else ma.zona end,ma.telefone=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.telefone else ma.telefone end,ma.contacto=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.contacto else ma.contacto end,ma.email=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.email else ma.email end,ma.obs=case when ISNULL(ts2.ocliequi,0)=1 And inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos inner join ts2 on ts.tsstamp=ts2.ts2stamp where inserted.mastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,nome,no,morada,local,codpost,zona,estab,vendnm,vendedor,instal,telefone,contacto,email,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao, inserted.nome,inserted.no,bo.morada,bo.local,bo.codpost,bo.zona,bo.estab,bo.vendnm,bo.vendedor,bo.dataobra,cl.telefone,cl.contacto,cl.email,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='CL' and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome='',ma.no=0,ma.morada='',ma.local='',ma.codpost='',ma.zona='',ma.estab=0, ma.vendnm='',ma.vendedor=0,ma.telefone='',ma.email='',ma.contacto='' from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 where inserted.ctrmastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='CL' and not inserted.ctrmastamp='' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL')>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,nome,no,morada,local,codpost,zona,estab,vendnm,vendedor,instal,telefone,contacto,email,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao,case when ts.actprodclp=1 then inserted.nome else '' end,case when ts.actprodclp=1 then inserted.no else 0 end,case when ts.actprodclp=1 then bo.morada else '' end,case when ts.actprodclp=1 then bo.local else '' end,case when ts.actprodclp=1 then bo.codpost else '' end,case when ts.actprodclp=1 then bo.zona else '' end,case when ts.actprodclp=1 then bo.estab else 0 end,case when ts.actprodclp=1 then bo.vendnm else '' end,case when ts.actprodclp=1 then bo.vendedor else 0 end,case when ts.actprodclp=1 then bo.dataobra else '' end,case when ts.actprodclp=1 then cl.telefone else '' end,case when ts.actprodclp=1 then cl.contacto else '' end,case when ts.actprodclp=1 then cl.email else '' end,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and (inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL') and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SET ma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='BO',ma.bostampv=inserted.bostamp,ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra,ma.nome=case when ts.actprodclc=1 then inserted.nome else '' end,ma.no=case when ts.actprodclc=1 then inserted.no else 0 end,ma.morada=case when ts.actprodclc=1 then bo.morada else '' end,ma.local=case when ts.actprodclc=1 then bo.local else '' end,ma.codpost=case when ts.actprodclc=1 then bo.codpost else '' end,ma.zona=case when ts.actprodclc=1 then bo.zona else '' end,ma.estab=case when ts.actprodclc=1 then bo.estab else 0 end,ma.vendnm=case when ts.actprodclc=1 then bo.vendnm else '' end,ma.vendedor=case when ts.actprodclc=1 then bo.vendedor else 0 end,ma.instal=case when ts.actprodclc=1 then bo.dataobra else '' end,ma.telefone=case when ts.actprodclc=1 then cl.telefone else '' end,ma.contacto=case when ts.actprodclc=1 then cl.contacto else '' end,ma.email=case when ts.actprodclc=1 then cl.email else '' end,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos where inserted.mastamp=ma.mastamp and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp='CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL')>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and (inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL') and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp<>'CL')>0 BEGIN UPDATE MA SET ma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='BO',ma.bostampv=inserted.bostamp,ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp<>'CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='FL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.ftnmdoc=inserted.nmdos,ma.ftfno=inserted.obrano,ma.ftfdata=inserted.dataobra,ma.bostampv=inserted.bostamp,ma.ftstamp='BO',ma.casa=0,ma.retfor=1,ma.armfornout=inserted.dataobra,ma.flnomea=inserted.nome,ma.flnoa=inserted.no,ma.flestaba=inserted.estab, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='FL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL' and inserted.ocliequi=1)>0 and exists (select boma.ocliequi from boma,inserted where boma.bostamp=inserted.bostamp And boma.bistamp=inserted.bistamp And boma.ocliequi=1) BEGIN UPDATE BOMA SET boma.ocliequi=0 from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 where inserted.bostamp=boma.bostamp And inserted.bistamp=boma.bistamp and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0 And exists (select boma.ocliequi from boma where boma.bostamp=inserted.bostamp And boma.bistamp=inserted.bistamp And boma.ocliequi=1)END IF (select COUNT(*) from inserted where inserted.serprvef = 1) > 0 UPDATE MA SET ma.serie = inserted.serie from inserted where inserted.serprvef = 1 and inserted.mastamp=ma.mastamp END if (select count(*) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))>0 BEGIN IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='FL') >0 BEGIN UPDATE MA SET ma.casa=1,ma.retfor=0,ma.armfornout='',ma.flnomea='',ma.flnoa=0,ma.flestab=0,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.bostampv='',ma.ftorigem='' from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='FL' and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=1) >0 BEGIN UPDATE MA SET ma.casa=0,ma.retfor=1,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.mastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0) >0 BEGIN DELETE FROM MA FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.bomastamp=ma.bomastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and not deleted.ctrmastamp='') >0 BEGIN UPDATE MA SETma.indisponivel=0 from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.ctrmastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and not deleted.ctrmastamp='' and deleted.emconf=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=0) >0 BEGIN UPDATE MA SET ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.nome='',ma.no=0,ma.estab=0,ma.morada='',ma.local='',ma.codpost='',ma.zona='',ma.vendnm='', ma.vendedor=0,ma.instal='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.telefone='',ma.contacto='',ma.email='', ma.ftorigem='',ma.casa=1,ma.bostampv='' from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=0 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=1) >0 BEGIN UPDATE MA SET ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.casa=1,ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata=''  from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='CL') >0 BEGIN UPDATE MA SET ma.casa=0,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where deleted.mastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='CL' and deleted.emconf=0 DELETE FROM MA FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where ma.bomastamp=deleted.bomastamp and deleted.mastamp='' and deleted.foserie=1 and deleted.bdemp='CL' and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=0 and deleted.refprod=1)>0 BEGIN UPDATE MA SET ma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.casa=1,ma.indisponivel=0,ma.ftorigem='',ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.nome=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.nome end,ma.no=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.no end,ma.morada=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.morada end,ma.local=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.local end,ma.codpost=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.codpost end,ma.zona=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.zona end,ma.estab=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.estab end,ma.vendnm=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.vendnm end,ma.vendedor=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.vendedor end,ma.telefone=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.telefone end,ma.contacto=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.contacto end,ma.email=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.email end,ma.instal=case when deleted.bdemp='CL' then '' else ma.instal end,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 inner join bo on deleted.bostamp=bo.bostamp inner join ts on bo.ndos=ts.ndos where deleted.mastamp=ma.mastamp and deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=0 and deleted.refprod=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=1 and deleted.refprod=1) >0 BEGIN DELETE FROM MA FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 where ma.bomastamp=deleted.bomastamp and deleted.mastamp='' and deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=1 and deleted.refprod=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.trfarm=1)>0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.noarm=deleted.armazem,ma.armdatain=deleted.dataobra, ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 inner join bo on deleted.bostamp=bo.bostamp where deleted.mastamp=ma.mastamp and deleted.trfarm=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and not deleted.ctrmastamp='') >0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.nome=deleted.nome,ma.no=deleted.no, ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.estab=bo.estab,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor, ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=1 inner join bo on deleted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where deleted.ctrmastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and not deleted.ctrmastamp='' and ma.no=0 and deleted.emconf=0 END IF (select COUNT(*) from deleted where deleted.serprvef = 1) > 0 UPDATE MA SET ma.serie = deleted.maserierpl from deleted where deleted.serprvef = 1 and deleted.mastamp=ma.mastamp END if (select count(*) from deleted inner join inserted on deleted.bistamp=inserted.bistamp where deleted.emconf=0 and inserted.emconf=0)>0 BEGIN IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=0)>0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.nome='',ma.no=0,ma.estab=0,ma.morada='',ma.local='',ma.codpost='',ma.zona='',ma.vendnm='', ma.vendedor=0,ma.instal='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.telefone='',ma.contacto='',ma.email='', ma.ftorigem='',ma.casa=1,ma.bostampv=''  from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=1)>0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.ftorigem='',ma.casa=1,ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata=''  from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=1 END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome=inserted.nome,ma.no=inserted.no,ma.bostampv=inserted.bostamp,ma.casa=0,ma.estab=inserted.estab,ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor,ma.instal=bo.dataobra, ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra,ma.ftorigem='BO', ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where inserted.mastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='CL' END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='FL')>0 BEGIN UPDATE MA SETma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.bostampv='',ma.ftorigem='',ma.casa=1,ma.retfor=0,ma.armfornout='',ma.flnomea='',ma.flnoa=0,ma.flestab=0,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='FL' END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='FL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.ftnmdoc=inserted.nmdos,ma.ftfno=inserted.obrano,ma.ftfdata=inserted.dataobra,ma.bostampv=inserted.bostamp,ma.ftorigem='BO',ma.casa=0,ma.retfor=1,ma.armfornout=inserted.dataobra,ma.flnomea=inserted.nome,ma.flnoa=inserted.no,ma.flestaba=inserted.estab, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='FL' END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0)>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnome,flno,flestab,flnmdoc,fladoc,fldata,obs,foorigem,situacao) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto, 1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4, inserted.nome,inserted.no,inserted.estab,inserted.nmdos,inserted.obrano,inserted.dataobra, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end,'BO',inserted.situacao from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where not exists (select bomastamp from ma where ma.bomastamp=inserted.bomastamp) and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata, ma.serie=inserted.serie,ma.serie2=inserted.serie2,ma.marca=inserted.marca,ma.maquina=inserted.maquina,ma.tipo=inserted.tipo,ma.fref=inserted.fref,ma.ccusto=inserted.ccusto,ma.ncusto=inserted.ncusto,ma.bostamp=inserted.bostamp,ma.bostampc=inserted.bostamp,ma.casa=1,ma.foorigem='BO',ma.situacao=inserted.situacao, ma.user1=inserted.user1,ma.user2=inserted.user2,ma.user3=inserted.user3,ma.user4=inserted.user4,ma.ref=inserted.ref,ma.design=inserted.design,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end, ma.flnome=inserted.nome,ma.flno=inserted.no,ma.flestab=inserted.estab,ma.flnmdoc=inserted.nmdos,ma.fladoc=inserted.obrano,ma.fldata=inserted.dataobra from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where ma.bomastamp=inserted.bomastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=1)>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata, ma.casa=1,ma.retfor=0,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra,  ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=1 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=1)>0 BEGIN UPDATE MA SETma.casa=0,ma.retfor=1,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp where deleted.mastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=1 END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=1,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.nome=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then inserted.nome else ma.nome end,ma.no=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then inserted.no else ma.no end,ma.estab=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.estab else ma.estab end,ma.morada=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.morada else ma.morada end,ma.local=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.local else ma.local end,ma.codpost=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.codpost else ma.codpost end,ma.zona=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.zona else ma.zona end,ma.telefone=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.telefone else ma.telefone end,ma.contacto=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.contacto else ma.contacto end,ma.email=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.email else ma.email end,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab join ts on bo.ndos=ts.ndos inner join ts2 on ts.tsstamp=ts2.ts2stamp where inserted.mastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='CL'END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='CL' and deleted.ocliequi=0)>0 BEGIN UPDATE MA SETma.casa=0,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp where deleted.mastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='CL' and deleted.ocliequi=0END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='CL')>0 BEGIN DELETE FROM MA FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where ma.bomastamp=deleted.bomastamp and deleted.mastamp='' and deleted.foserie=1 and deleted.bdemp='CL' END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata, ma.serie=inserted.serie,ma.serie2=inserted.serie2,ma.marca=inserted.marca,ma.maquina=inserted.maquina,ma.tipo=inserted.tipo,ma.fref=inserted.fref,ma.ccusto=inserted.ccusto,ma.ncusto=inserted.ncusto,ma.bostamp=inserted.bostamp,ma.bostampc=inserted.bostamp,ma.casa=1,ma.foorigem='BO',ma.situacao=inserted.situacao, ma.user1=inserted.user1,ma.user2=inserted.user2,ma.user3=inserted.user3,ma.user4=inserted.user4,ma.ref=inserted.ref,ma.design=inserted.design,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end, ma.flnmdoc=inserted.nmdos,ma.fladoc=inserted.obrano,ma.fldata=inserted.dataobra,ma.nome=inserted.nome,ma.no=inserted.no,ma.estab=inserted.estab,ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor,ma.instal=bo.dataobra,ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where ma.bomastamp=inserted.bomastamp and inserted.mastamp='' and inserted.foserie=1 and inserted.bdemp='CL' INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,nome,no,morada,local,codpost,zona,estab,vendnm,vendedor,instal,telefone,contacto,email,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao, inserted.nome,inserted.no,bo.morada,bo.local,bo.codpost,bo.zona,bo.estab,bo.vendnm,bo.vendedor,bo.dataobra,cl.telefone,cl.contacto,cl.email,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and inserted.foserie=1 and inserted.bdemp='CL' and inserted.retequi=0 END IF (select count(*) from deleted where deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=1 and deleted.refprod=1)>0 BEGIN DELETE FROM MA FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where ma.bomastamp=deleted.bomastamp and deleted.mastamp='' and deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=1 and deleted.refprod=1 END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata, ma.serie=inserted.serie,ma.serie2=inserted.serie2,ma.marca=inserted.marca,ma.maquina=inserted.maquina,ma.tipo=inserted.tipo,ma.fref=inserted.fref,ma.ccusto=inserted.ccusto,ma.ncusto=inserted.ncusto,ma.bostamp=inserted.bostamp,ma.bostampc=inserted.bostamp,ma.casa=1,ma.foorigem='BO',ma.situacao=inserted.situacao, ma.user1=inserted.user1,ma.user2=inserted.user2,ma.user3=inserted.user3,ma.user4=inserted.user4,ma.ref=inserted.ref,ma.design=inserted.design,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end, ma.flnmdoc=inserted.nmdos,ma.fladoc=inserted.obrano,ma.fldata=inserted.dataobra,ma.nome=case when ts.actprodclp=1 then inserted.nome else ma.nome end,ma.no=case when ts.actprodclp=1 then inserted.no else ma.no end,ma.morada=case when ts.actprodclp=1 then bo.morada else ma.morada end,ma.local=case when ts.actprodclp=1 then bo.local else ma.local end,ma.codpost=case when ts.actprodclp=1 then bo.codpost else ma.codpost end,ma.zona=case when ts.actprodclp=1 then bo.zona else ma.zona end,ma.estab=case when ts.actprodclp=1 then inserted.estab else ma.estab end,ma.vendnm=case when ts.actprodclp=1 then bo.vendnm else ma.vendnm end,ma.vendedor=case when ts.actprodclp=1 then bo.vendedor else ma.vendedor end,ma.instal=case when ts.actprodclp=1 then bo.dataobra else ma.instal end,ma.telefone=case when ts.actprodclp=1 then cl.telefone else ma.telefone end,ma.contacto=case when ts.actprodclp=1 then cl.contacto else ma.contacto end,ma.email=case when ts.actprodclp=1 then cl.email else ma.email end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where ma.bomastamp=inserted.bomastamp and inserted.mastamp='' and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL' INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,nome,no,morada,local,codpost,zona,estab,vendnm,vendedor,instal,telefone,contacto,email,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao,case when ts.actprodclp=1 then inserted.nome else '' end,case when ts.actprodclp=1 then inserted.no else 0 end,case when ts.actprodclp=1 then bo.morada else '' end,case when ts.actprodclp=1 then bo.local else '' end,case when ts.actprodclp=1 then bo.codpost else '' end,case when ts.actprodclp=1 then bo.zona else '' end,case when ts.actprodclp=1 then bo.estab else 0 end,case when ts.actprodclp=1 then bo.vendnm else '' end,case when ts.actprodclp=1 then bo.vendedor else 0 end,case when ts.actprodclp=1 then bo.dataobra else '' end,case when ts.actprodclp=1 then cl.telefone else '' end,case when ts.actprodclp=1 then cl.contacto else '' end,case when ts.actprodclp=1 then cl.email else '' end,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and (inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL') END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata, ma.serie=inserted.serie,ma.serie2=inserted.serie2,ma.marca=inserted.marca,ma.maquina=inserted.maquina,ma.tipo=inserted.tipo,ma.fref=inserted.fref,ma.ccusto=inserted.ccusto,ma.ncusto=inserted.ncusto,ma.bostamp=inserted.bostamp,ma.bostampc=inserted.bostamp,ma.casa=1,ma.foorigem='BO',ma.situacao=inserted.situacao, ma.user1=inserted.user1,ma.user2=inserted.user2,ma.user3=inserted.user3,ma.user4=inserted.user4,ma.ref=inserted.ref,ma.design=inserted.design,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end, ma.flnmdoc=inserted.nmdos,ma.fladoc=inserted.obrano,ma.fldata=inserted.dataobra,ma.nome=inserted.nome,ma.no=inserted.no,ma.estab=inserted.estab from inserted inner join bo on inserted.bostamp=bo.bostamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where ma.bomastamp=inserted.bomastamp and inserted.mastamp='' and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL' INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,obs)select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and (inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL') END IF (select count(*) from deleted where deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=0 and deleted.refprod=1)>0 BEGIN UPDATE MA SET ma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.casa=1,ma.indisponivel=0,ma.ftorigem='',ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.nome=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.nome end,ma.no=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.no end,ma.morada=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.morada end,ma.local=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.local end,ma.codpost=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.codpost end,ma.zona=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.zona end,ma.estab=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.estab end,ma.vendnm=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.vendnm end,ma.vendedor=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.vendedor end,ma.telefone=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.telefone end,ma.contacto=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.contacto end,ma.email=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.email end,ma.instal=case when deleted.bdemp='CL' then '' else ma.instal end,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp inner join ts on bo.ndos=ts.ndos where deleted.mastamp=ma.mastamp and deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=0 and deleted.refprod=1 END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SET ma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='BO',ma.bostampv=inserted.bostamp,ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra,ma.nome=case when ts.actprodclc=1 then inserted.nome else ma.nome end,ma.no=case when ts.actprodclc=1 then inserted.no else ma.no end,ma.morada=case when ts.actprodclc=1 then bo.morada else ma.morada end,ma.local=case when ts.actprodclc=1 then bo.local else ma.local end,ma.codpost=case when ts.actprodclc=1 then bo.codpost else ma.codpost end,ma.zona=case when ts.actprodclc=1 then bo.zona else ma.zona end,ma.estab=case when ts.actprodclc=1 then bo.estab else ma.estab end,ma.vendnm=case when ts.actprodclc=1 then bo.vendnm else ma.vendnm end,ma.vendedor=case when ts.actprodclc=1 then bo.vendedor else ma.vendedor end,ma.instal=case when ts.actprodclc=1 then bo.dataobra else ma.instal end,ma.telefone=case when ts.actprodclc=1 then cl.telefone else ma.telefone end,ma.contacto=case when ts.actprodclc=1 then cl.contacto else ma.contacto end,ma.email=case when ts.actprodclc=1 then cl.email else ma.email end,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos where inserted.mastamp=ma.mastamp and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp='CL' END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp<>'CL')>0 BEGIN UPDATE MA SET ma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='BO',ma.bostampv=inserted.bostamp,ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp<>'CL' END IF (select count(*) from deleted where deleted.trfarm=1)>0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.noarm=deleted.armazem,ma.armdatain=deleted.dataobra, ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp where deleted.mastamp=ma.mastamp and deleted.trfarm=1 END IF (select count(*) from inserted where inserted.trfarm=1)>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.noarm=inserted.ar2mazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end  from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.trfarm=1 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and not deleted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.nome=deleted.nome,ma.no=deleted.no, ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.estab=bo.estab,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor, ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where deleted.ctrmastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and not deleted.ctrmastamp='' END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='CL' and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome='',ma.no=0,ma.morada='',ma.local='',ma.codpost='',ma.zona='',ma.estab=0, ma.vendnm='',ma.vendedor=0,ma.telefone='',ma.email='',ma.contacto='' from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ctrmastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='CL' and not inserted.ctrmastamp='' and ma.no = 0 END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.indisponivel=1 from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ctrmastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='' END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and not deleted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.indisponivel=0 from deleted inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ctrmastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and not deleted.ctrmastamp=''END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL')>0 and exists (select boma.ocliequi from boma,inserted where boma.bostamp=inserted.bostamp And boma.bistamp=inserted.bistamp And boma.ocliequi=1)BEGIN UPDATE BOMA SET boma.ocliequi=0 from inserted where inserted.bostamp=boma.bostamp And inserted.bistamp=boma.bistamp and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0 And exists (select boma.ocliequi from boma where boma.bostamp=inserted.bostamp And boma.bistamp=inserted.bistamp And boma.ocliequi=1)END END END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bi_insert ON bi WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.emconf=0 and bo2.anulado=0)>0
BEGIN
IF ( select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on inserted.ndos=ts.ndos where ts.stocks=1 and inserted.ref<>space(18) and bo2.anulado=0)>0
INSERT INTO SL
(slstamp,bistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,
fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,
ref,armazem,qtt,
lote,
tt,
vu,
ett,
evu,
num1,
frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,
vumoeda,
ttmoeda,
pcpond,cpoc,
valiva,trfa,evaliva,epcpond,stns
)
select inserted.bistamp,inserted.bistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BO',
case when ts.lifref=1 then inserted.bifref else bo.fref end,case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,bo.nome,inserted.codigo,bo.moeda,inserted.rdata,inserted.design,
inserted.ref,inserted.armazem,inserted.qtt,
inserted.lote,
inserted.sltt,
inserted.slvu,
inserted.esltt,
inserted.eslvu,
inserted.num1,
bo.no,convert(char (10), bo.obrano),
case when (ts.producao=1 and inserted.producao=1) Or (ts2.trocaequi=1 and inserted.trocaequi=1) then ts.cm2stocks else ts.cmstocks end,
case when (ts.producao=1 and inserted.producao=1) Or (ts2.trocaequi=1 and inserted.trocaequi=1) then ts.cm2desc else ts.cmdesc end,
inserted.composto,inserted.cor,inserted.tam,
inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,
inserted.slvumoeda,
inserted.slttmoeda,
inserted.pcusto,inserted.cpoc,
case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end,ts.trfa,
case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,
inserted.epcusto,inserted.stns
from inserted
inner join bo on bo.bostamp=inserted.bostamp inner join ts on inserted.ndos=ts.ndos inner join ts2 on ts2.ts2stamp=ts.tsstamp
 inner join bo2 on bo2.bo2stamp=inserted.bostamp
where ts.stocks=1 and inserted.ref<>'' and inserted.emconf=0 and bo2.anulado=0
IF (select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on inserted.ndos=ts.ndos where ts.stocks=1 and ts.trfa=1 and inserted.ref<>'' and bo2.anulado=0 )>0
INSERT INTO SL
(slstamp,bistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,
fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,
ref,armazem,qtt,
lote,
tt,
vu,
ett,
evu,
num1,
frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,
vumoeda,
ttmoeda,
pcpond,cpoc,
valiva,trfa,evaliva,epcpond,stns
)
select substring('T'+inserted.bistamp,1,25),inserted.bistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BO',
case when ts.lifref=1 then inserted.bifref else bo.fref end,case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,bo.nome,inserted.codigo,bo.moeda,inserted.rdata,inserted.design,
inserted.ref,inserted.ar2mazem,inserted.qtt,
inserted.lote,
inserted.sltt,
inserted.slvu,
inserted.esltt,
inserted.eslvu,
inserted.num1,
bo.no,convert(char (10), bo.obrano),ts.cm2stocks,ts.cm2desc,inserted.composto,inserted.cor,inserted.tam,
inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,
inserted.slvumoeda,
inserted.slttmoeda,
inserted.pcusto,inserted.cpoc,
case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end,ts.trfa,
case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,
inserted.epcusto,inserted.stns
from inserted
 inner join bo2 on bo2.bo2stamp=inserted.bostamp
inner join bo on bo.bostamp=inserted.bostamp inner join ts on inserted.ndos=ts.ndos where ts.stocks=1 and ts.trfa=1  and inserted.ref<>space(18) and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttcli=st.qttcli+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref) i
where st.ref=i.ref
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttcat=st.qttcat+(case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end)
from st inner join inserted on st.ref=inserted.ref inner join bo2 on inserted.bostamp = bo2.bo2stamp
where inserted.ref<>'' and st.ref=inserted.ref and inserted.cativo=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttfor=st.qttfor+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref) i
where st.ref=i.ref
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resusr=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.usrqtt=st.usrqtt+(case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end)
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
where inserted.ref<>'' and st.ref=inserted.ref and inserted.resusr=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttrec=st.qttrec+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref) i
where st.ref=i.ref
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.fechada=0 and bo2.anulado=0 )>0
INSERT INTO SA (sastamp,armazem,ref) select convert(char(5),inserted.armazem)+inserted.ref,inserted.armazem,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
where not EXISTS(select sa.armazem from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.ref<>'' and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.rescli=sa.rescli+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if (select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.rescat=sa.rescat+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.resfor=sa.resfor+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.resfor=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.qttrec=sa.qttrec+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.resrec=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
INSERT INTO SX (sxstamp,armazem,ref,cor,tam,rescli ,resfor ,qttrec,ststamp) select substring(cast(newid() as char(36)),1,25),inserted.armazem,inserted.ref,inserted.cor,inserted.tam,0,0,0,st.ststamp
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join st on inserted.ref=st.ref
where inserted.cor<>'' and inserted.tam<>''
and not EXISTS(select armazem from sx where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam) and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1)
and inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.emconf=0 and bo2.anulado=0
GROUP BY inserted.armazem,inserted.ref,inserted.cor,inserted.tam,st.ststamp
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
UPDATE Sx SET
sx.rescli =sx.rescli +(case when i.valor >0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem, inserted.cor,inserted.tam
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.rescli=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem,inserted.cor,inserted.tam) i
where sx.ref=i.ref and sx.armazem=i.armazem and sx.cor = i.cor and sx.tam = i.tam
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
UPDATE Sx SET
sx.resfor =sx.resfor+(case when i.valor >0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem, inserted.cor,inserted.tam
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp

where inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.resfor=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem,inserted.cor,inserted.tam) i
where sx.ref=i.ref and sx.armazem=i.armazem and sx.cor = i.cor and sx.tam = i.tam
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
UPDATE Sx SET
sx.qttrec =sx.qttrec +(case when i.valor >0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem, inserted.cor,inserted.tam
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp 

where inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.resrec=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0 
group by inserted.ref,inserted.armazem,inserted.cor,inserted.tam) i
where sx.ref=i.ref and sx.armazem=i.armazem and sx.cor = i.cor and sx.tam = i.tamif ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0  UPDATE Sx SET sx.rescat=sx.rescat+(case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and inserted.cativo=1  and inserted.fechada=0 and inserted.emconf=0 and inserted.emconf=0 and bo2.anulado=0 IF (select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.obistamp<>'' and inserted.qtt<>0 and bo2.anulado=0)>0 BEGIN UPDATE BI SET bi.fdata = bo.dataobra,bi.fno = bo.obrano,bi.adoc = convert(char(10),bo.obrano),bi.nmdoc = bo.nmdos,bi.ndoc = bo.ndos, bi.qtt2 = bi.qtt2+(select SUM(i.qtt) from inserted i where i.obistamp=inserted.obistamp group by i.obistamp) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join bo (nolock) on inserted.bostamp=bo.bostamp where inserted.obistamp<>'' and inserted.obistamp=bi.bistamp and inserted.emconf=0 and bo2.anulado=0 UPDATE BI SET bi.partes2 = bi.partes2+inserted.partes from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join bi (nolock) on bi.bistamp = inserted.obistampinner join ts (nolock) on ts.ndos = bi.ndos where inserted.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and bo2.anulado=0 END END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_re_update ON RE WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif update(usrhora)
or update(chdata) or update(no) or update(nome) or update(total) or update(etotal) or update(clbanco) or update(clcheque) or update(procdata)
or update(cobranca) or update(difcambio) or update(edifcambio) or update(total) or update(arred) or update(etotal) or update(earred) or update(moeda)
or update(rno) or update(procdata) or update(fref) or update(intid) or update(cobrador) or update(rota) or update(ccusto) or update(ncusto)
or update(zona) or update(vendedor) or update(vendnm) or update(segmento) or update(tipo) or update(pais) or update(estab) or update(desc1)
or update(totol2) or update(etotol2) or update(ollocal) or update(contado) or update(ollocal2) or update(contado2) or update(telocal)
or update(telocal2) or update(olcodigo) or update(finv) or update(process)
BEGIN

if (select count(*) from inserted where inserted.process=1 and inserted.restamp not in (select cc.ccstamp from cc where cc.ccstamp=inserted.restamp))>0
insert into CC
(ccstamp,restamp,facstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,difcambio,edifcambio
,cred,ecred,credf,ecredf,credm,credfm,deb,edeb,debf,edebf,debm,debfm
,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs,incobra
)
select inserted.restamp,inserted.restamp,inserted.facstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora
,inserted.ousrinis,inserted.procdata,inserted.no,inserted.nome
, (case when exists (select cm from cm1 (nolock) where cm =(select cmcc from tsre (nolock) where ndoc=inserted.ndoc))
then (select cmcc from tsre (nolock) where ndoc=inserted.ndoc)
else 70 end)
, (case when exists (select cmdesc from cm1 (nolock) where cm=(select cmcc from tsre (nolock) where ndoc=inserted.ndoc))
then (select cmccn from tsre (nolock) where ndoc=inserted.ndoc)
else (select cmdesc from cm1 (nolock) where cm=70) end)
, 'RE',inserted.cobranca,inserted.difcambio,inserted.edifcambio
, case when inserted.total-inserted.arred<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal-inserted.earred<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.total-inserted.arred<0 then 0 else inserted.total-inserted.arred end
, case when inserted.etotal-inserted.earred<0 then 0 else inserted.etotal-inserted.earred end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.totalmoeda<0 then 0 else inserted.totalmoeda end
, case when inserted.total-inserted.arred>0 then 0 else -(inserted.total-inserted.arred) end
, case when inserted.etotal-inserted.earred>0 then 0 else -(inserted.etotal-inserted.earred) end
, case when inserted.total-inserted.arred>0 then 0 else -(inserted.total-inserted.arred) end
, case when inserted.etotal-inserted.earred>0 then 0 else -(inserted.etotal-inserted.earred) end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, case when inserted.totalmoeda>0 then 0 else -inserted.totalmoeda end
, inserted.moeda,inserted.rno,inserted.procdata,inserted.fref,inserted.intid,inserted.cobrador,inserted.rota,inserted.ccusto,inserted.ncusto
, inserted.zona,inserted.vendedor,inserted.vendnm,inserted.segmento,inserted.tipo,inserted.pais,inserted.estab, inserted.desc1
, (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)

from inserted
where inserted.process=1 and inserted.restamp not in (select cc.ccstamp from cc where cc.ccstamp=inserted.restamp)

if (select count(*) from inserted where inserted.process=1 and inserted.restamp in (select cc.ccstamp from cc where cc.ccstamp=inserted.restamp))>0
update CC
SET cc.ccstamp =inserted.restamp,cc.restamp=inserted.restamp,cc.facstamp=inserted.facstamp
, cc.usrdata=inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis
, cc.datalc=inserted.procdata,cc.no=inserted.no,cc.nome=inserted.nome
, cc.cm =(case when exists (select cm from cm1 (nolock) where cm=(select cmcc from tsre (nolock) where ndoc=inserted.ndoc))
then (select cmcc from tsre (nolock) where ndoc=inserted.ndoc)
else 70 end)
, cc.cmdesc =(case when exists (select cmdesc from cm1 (nolock) where cm =(select cmcc from tsre (nolock) where ndoc=inserted.ndoc))
then (select cmccn from tsre (nolock) where ndoc=inserted.ndoc) else (select cmdesc from cm1 (nolock) where cm =70) end)
, cc.origem ='RE', cc.cobranca=inserted.cobranca
, cc.difcambio=inserted.difcambio,cc.edifcambio=inserted.edifcambio
, cc.cred =case when inserted.total-inserted.arred<0 then cc.cred else inserted.total-inserted.arred end
, cc.ecred =case when inserted.etotal-inserted.earred<0 then cc.ecred else inserted.etotal-inserted.earred end
, cc.credf =case when inserted.total-inserted.arred<0 then cc.credf else inserted.total-inserted.arred end
, cc.ecredf =case when inserted.etotal-inserted.earred<0 then cc.ecredf else inserted.etotal-inserted.earred end
, cc.credm =case when inserted.totalmoeda<0 then cc.credm else inserted.totalmoeda end
, cc.credfm =case when inserted.totalmoeda<0 then cc.credfm else inserted.totalmoeda end
, cc.deb =case when inserted.total-inserted.arred>0 then cc.deb else -(inserted.total-inserted.arred) end
, cc.edeb=case when inserted.etotal-inserted.earred>0 then cc.edeb else -(inserted.etotal-inserted.earred) end
, cc.debf =case when inserted.total-inserted.arred>0 then cc.debf else -(inserted.total-inserted.arred) end
, cc.edebf=case when inserted.etotal-inserted.earred>0 then cc.edebf else -(inserted.etotal-inserted.earred) end
, cc.debm =case when inserted.totalmoeda>0 then cc.debm else -inserted.totalmoeda end
, cc.debfm =case when inserted.totalmoeda>0 then cc.debfm else -inserted.totalmoeda end
, cc.moeda =inserted.moeda,cc.nrdoc=inserted.rno,cc.dataven=inserted.procdata
, cc.fref =inserted.fref,cc.intid=inserted.intid,cc.cobrador=inserted.cobrador,cc.rota=inserted.rota,cc.ccusto=inserted.ccusto
, cc.ncusto =inserted.ncusto,cc.zona=inserted.zona
, cc.vendedor =inserted.vendedor,cc.vendnm=inserted.vendnm,cc.segmento=inserted.segmento,cc.tipo=inserted.tipo,cc.pais=inserted.pais,cc.estab=inserted.estab,cc.obs=inserted.desc1
, cc.incobra = (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)

from inserted
where inserted.process=1
and inserted.restamp in (select cc.ccstamp from cc where cc.ccstamp=inserted.restamp) and inserted.restamp=cc.ccstamp

DELETE FROM CC FROM inserted
where inserted.process=0
and substring('I'+inserted.restamp,1,25) in (select cc.ccstamp from cc where cc.ccstamp=substring('I'+inserted.restamp,1,25)) and substring('I'+inserted.restamp,1,25)=cc.ccstamp

INSERT INTO CC
(ccstamp,restamp,facstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem
,cred,ecred,credf,ecredf,deb,edeb,debf,edebf
,moeda,nrdoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs,incobra)
select substring('I'+inserted.restamp,1,25),inserted.restamp,inserted.facstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis
, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis, inserted.procdata,inserted.no,inserted.nome, 0
, 'Indefinido!'
, 'RE'
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs<0 then 0 else inserted.virs end
, case when inserted.evirs<0 then 0 else inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, case when inserted.virs>0 then 0 else -inserted.virs end
, case when inserted.evirs>0 then 0 else -inserted.evirs end
, inserted.moeda,inserted.rno,inserted.procdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo
, inserted.estab,inserted.pais,inserted.zona,inserted.desc1
, (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)
from inserted
where inserted.process=1
and (substring('I'+inserted.restamp,1,25) not in (select cc.ccstamp from cc where cc.ccstamp=substring('I'+inserted.restamp,1,25))) and (inserted.virs<>0 or inserted.evirs<>0)

UPDATE CC
set cc.ccstamp =substring('I'+inserted.restamp,1,25),cc.restamp=inserted.restamp,cc.facstamp=inserted.facstamp
, cc.usrdata =inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis
, cc.datalc =inserted.procdata,cc.no=inserted.no,cc.nome=inserted.nome
, cc.cm =0
, cc.cmdesc ='Indefinido!'
, cc.origem='RE'
, cc.cred =case when inserted.virs<0 then 0 else inserted.virs end
, cc.ecred =case when inserted.evirs<0 then 0 else inserted.evirs end
, cc.credf =case when inserted.virs<0 then 0 else inserted.virs end
, cc.ecredf =case when inserted.evirs<0 then 0 else inserted.evirs end
, cc.deb =case when inserted.virs>0 then 0 else -inserted.virs end
, cc.edeb =case when inserted.evirs>0 then 0 else -inserted.evirs end
, cc.debf =case when inserted.virs>0 then 0 else -inserted.virs end
, cc.edebf =case when inserted.evirs>0 then 0 else -inserted.evirs end
, cc.moeda =inserted.moeda,cc.nrdoc=inserted.rno,cc.dataven=inserted.procdata
, cc.fref =inserted.fref,cc.ccusto=inserted.ccusto,cc.ncusto=inserted.ncusto,cc.intid=inserted.intid,cc.tipo=inserted.tipo
, cc.estab =inserted.estab,cc.pais=inserted.pais,cc.zona=inserted.zona,cc.obs=inserted.desc1
, cc.incobra = (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)
from inserted
where inserted.process=1
and substring('I'+inserted.restamp,1,25) in (select cc.ccstamp from cc where cc.ccstamp=substring('I'+inserted.restamp,1,25))
and substring('I'+inserted.restamp,1,25)=cc.ccstamp and (inserted.virs<>0 or inserted.evirs<>0)

DELETE FROM CC FROM inserted
where inserted.process=0
and inserted.restamp in (select cc.ccstamp from cc where cc.ccstamp=inserted.restamp) and inserted.restamp=cc.ccstamp

if (select count(*) from inserted where inserted.process=1 and inserted.finv<>0)>0
INSERT INTO CC
(ccstamp,restamp,facstamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca
, cred,ecred,credf,ecredf,credm,credfm,deb,edeb,debf,edebf,debm,debfm
, moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,obs,estab)
select substring('F'+inserted.restamp,1,25),inserted.restamp,inserted.facstamp
, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
, inserted.procdata,inserted.no,inserted.nome, 94
,(select cmdesc from cm1 (nolock) where cm =94)
,'RE',inserted.cobranca
, case when inserted.finv<0 then 0 else inserted.finv end,case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finv<0 then 0 else inserted.finv end,case when inserted.efinv<0 then 0 else inserted.efinv end
, case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end,case when inserted.finvmoeda<0 then 0 else inserted.finvmoeda end
, case when inserted.finv>0 then 0 else -inserted.finv end,case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finv>0 then 0 else -inserted.finv end,case when inserted.efinv>0 then 0 else -inserted.efinv end
, case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end,case when inserted.finvmoeda>0 then 0 else -inserted.finvmoeda end
, inserted.moeda,inserted.rno,inserted.procdata,inserted.fref,inserted.intid,inserted.cobrador,inserted.rota,inserted.ccusto,inserted.ncusto,inserted.zona,inserted.vendedor,inserted.vendnm
, inserted.desc1,inserted.estab
from inserted
where inserted.process=1
and substring('F'+inserted.restamp,1,25) not in (select cc.ccstamp from cc where cc.ccstamp=substring('F'+inserted.restamp,1,25)) and inserted.finv<>0

if (select count(*) from inserted where inserted.process=1 and inserted.finv<>0)>0
UPDATE CC
SET cc.ccstamp=substring('F'+inserted.restamp,1,25),cc.restamp=inserted.restamp,cc.facstamp=inserted.facstamp
, cc.usrdata=inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis
, cc.datalc=inserted.procdata,cc.no=inserted.no,cc.nome=inserted.nome,cc.cm=94
, cc.cmdesc=(select cmdesc from cm1 (nolock) where cm =94)
, cc.origem='RE',cc.cobranca=inserted.cobranca
, cc.cred =case when inserted.finv<0 then cc.cred else inserted.finv end
, cc.ecred =case when inserted.efinv<0 then cc.ecred else inserted.efinv end
, cc.credf =case when inserted.finv<0 then cc.credf else inserted.finv end
, cc.ecredf =case when inserted.efinv<0 then cc.ecredf else inserted.efinv end
, cc.credm =case when inserted.finvmoeda<0 then cc.credm else inserted.finvmoeda end
, cc.credfm =case when inserted.finvmoeda<0 then cc.credfm else inserted.finvmoeda end
, cc.deb =case when inserted.finv>0 then cc.deb else -inserted.finv end
, cc.edeb =case when inserted.efinv>0 then cc.edeb else -inserted.efinv end
, cc.debf =case when inserted.finv>0 then cc.debf else -inserted.finv end
, cc.edebf =case when inserted.efinv>0 then cc.edebf else -inserted.efinv end
, cc.debm =case when inserted.finvmoeda>0 then cc.debm else -inserted.finvmoeda end
, cc.debfm =case when inserted.finvmoeda>0 then cc.debfm else -inserted.finvmoeda end
, cc.moeda =inserted.moeda,cc.nrdoc=inserted.rno,cc.dataven=inserted.procdata
, cc.fref =inserted.fref,cc.intid=inserted.intid,cc.cobrador=inserted.cobrador,cc.rota=inserted.rota
, cc.ccusto =inserted.ccusto,cc.ncusto=inserted.ncusto,cc.zona=inserted.zona,cc.vendedor=inserted.vendedor
, cc.vendnm=inserted.vendnm,cc.obs=inserted.desc1
from inserted
where inserted.process=1
and substring('F'+inserted.restamp,1,25) in (select cc.ccstamp from cc where cc.ccstamp=substring('F'+inserted.restamp,1,25))
and substring('F'+inserted.restamp,1,25)=cc.ccstamp
and inserted.finv<>0

DELETE FROM OL FROM inserted where inserted.process=0 and inserted.restamp=ol.restamp and inserted.revdata = '19000101'
DELETE FROM OL FROM inserted
where (inserted.process=0 or (inserted.totol2=0 and inserted.etotol2=0))
and substring('2'+inserted.restamp,1,25)=ol.restamp and inserted.revdata = '19000101'

if (select count(*) from inserted where inserted.process=1)>0
UPDATE OL
set ol.olstamp =inserted.restamp,ol.restamp=inserted.restamp
, ol.usrdata =inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis
, ol.ousrdata =inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='RE'
, ol.documento =(case when (select oldoc from tsre where ndoc=inserted.ndoc)=' ' then 'Recibo' else (select oldoc from tsre where ndoc=inserted.ndoc) end)
, ol.cheque=convert(char(15),inserted.rno),ol.data=inserted.procdata,ol.dvalor=inserted.procdata
, ol.entr =case when case when inserted.memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end<0 then 0
else case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end end
, ol.eentr =case when case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end<0 then 0
else case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end end
, ol.entrm =case when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2<0 then ol.entrm
else inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 end
, ol.said =case when case when inserted.memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow
else inserted.total-inserted.totol2+inserted.totow end>0 then ol.said
else -(case when inserted.memissao='EURO' then inserted.total-inserted.totol2-inserted.arred+inserted.totow else inserted.total-inserted.totol2+inserted.totow end) end
, ol.esaid =case when case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2+inserted.etotow end>0 then ol.esaid
else -(case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.earred+inserted.etotow else inserted.etotal-inserted.etotol2+inserted.etotow end) end
, ol.saidm =case when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2>0 then ol.saidm else -(inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2) end
, ol.moeda =inserted.moeda2, ol.descricao=inserted.nome,ol.olcodigo=inserted.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo
, ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal
, ol.contado=inserted.contado,ol.local=inserted.telocal
, ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp
, ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where ol.restamp=inserted.restamp and inserted.process=1 and ol.origem <> 'REREV'

if (select count(*) from inserted where inserted.process=1)>0
UPDATE OL
SET ol.olstamp =substring('2'+inserted.restamp,1,25),ol.restamp=substring('2'+inserted.restamp,1,25)
, ol.usrdata =inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis
, ol.ousrdata =inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='RE'
, ol.documento =(case when (select oldoc from tsre where ndoc=inserted.ndoc)=' ' then 'Recibo' else (select oldoc from tsre where ndoc=inserted.ndoc) end)
, ol.cheque =convert(char(15),inserted.rno),ol.data=inserted.procdata,ol.dvalor=inserted.procdata
, ol.entr =case when inserted.totol2<0 then ol.entr else inserted.totol2 end
, ol.eentr =case when inserted.etotol2<0 then ol.eentr else inserted.etotol2 end,ol.entrm=case when inserted.valor2m3<0 then ol.entrm else inserted.valor2m3 end
, ol.said =case when inserted.totol2>0 then ol.said else -inserted.totol2 end
, ol.esaid=case when inserted.etotol2>0 then ol.esaid else -inserted.etotol2 end
, ol.saidm=case when inserted.valor2m3>0 then ol.saidm else -inserted.valor2m3 end
, ol.moeda=inserted.moeda3
, ol.descricao=inserted.nome,ol.olcodigo=inserted.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo
, ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal2
, ol.contado=inserted.contado2,ol.local=inserted.telocal2, ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp
, ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where ol.restamp=substring('2'+inserted.restamp,1,25) and inserted.process=1 and ol.origem <> 'REREV'

DELETE FROM CC
FROM inserted
where inserted.process=0 and substring('F'+inserted.restamp,1,25) in (select cc.ccstamp from cc where cc.ccstamp=substring('F'+inserted.restamp,1,25)) and substring('F'+inserted.restamp,1,25)=cc.ccstamp

if (select count(*) from inserted where inserted.process=1
and 0 = (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)
and (case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end<>0
or  case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end<>0
or inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2<>0))>0
INSERT INTO OL
(olstamp,restamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque,data,dvalor
, entr,eentr,entrm,said,esaid,saidm, moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local
, site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select inserted.restamp,inserted.restamp, inserted.usrdata,inserted.usrhora,inserted.usrinis, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis
,'RE'
, (case when (select oldoc from tsre where ndoc=inserted.ndoc)=' ' then 'Recibo' else (select oldoc from tsre where ndoc=inserted.ndoc) end)
, convert(char(15),inserted.rno),inserted.procdata,inserted.procdata
, case when case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end<0 then 0
else case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end end
, case when case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end<0 then 0
else case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end end
, case when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2<0 then 0
else inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2 end
, case when case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end>0 then 0
else -(case when memissao='EURO' then inserted.total-inserted.totol2-inserted.totoladi-inserted.arred+inserted.totow
else inserted.total-inserted.totol2-inserted.totoladi+inserted.totow  end) end
, case when case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end>0 then 0
else -(case when inserted.memissao='PTE ' then inserted.etotal-inserted.etotol2-inserted.etotoladi-inserted.earred+inserted.etotow
else inserted.etotal-inserted.etotol2-inserted.etotoladi+inserted.etotow  end) end
, case when inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2>0 then 0
else -(inserted.valorm2-inserted.valor2m2-inserted.earredm2+inserted.valorowm2) end
, inserted.moeda2
, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado
, inserted.telocal, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername,inserted.ssstamp,inserted.ssusername,inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where inserted.process =1
and inserted.restamp not in (select ol.restamp from ol where ol.restamp=inserted.restamp)

if (select count(*) from inserted where inserted.process=1
and 0 = (select tsre.movinc from tsre (nolock) where inserted.ndoc = tsre.ndoc)
and (inserted.totol2<>0 or inserted.etotol2<>0 or inserted.valor2m3<>0))>0
INSERT INTO OL
(olstamp,restamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem, documento,cheque,data,dvalor
, entr,eentr,entrm,said,esaid,saidm, moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local
, site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select substring('2'+inserted.restamp,1,25),substring('2'+inserted.restamp,1,25)
, inserted.usrdata,inserted.usrhora,inserted.usrinis
, inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'RE'
, (case when (select oldoc from tsre where ndoc=inserted.ndoc)=' ' then 'Recibo' else (select oldoc from tsre where ndoc=inserted.ndoc) end)
, convert(char(15),inserted.rno),inserted.procdata,inserted.procdata
, case when inserted.totol2<0 then 0 else inserted.totol2 end
, case when inserted.etotol2<0 then 0 else inserted.etotol2 end
, case when inserted.valor2m3<0 then 0 else inserted.valor2m3 end
, case when inserted.totol2>0 then 0 else -inserted.totol2 end
, case when inserted.etotol2>0 then 0 else -inserted.etotol2 end
, case when inserted.valor2m3>0 then 0 else -inserted.valor2m3 end
, inserted.moeda3
, inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo, inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal2,inserted.contado2,inserted.telocal2
, inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername
, inserted.ssstamp,inserted.ssusername,inserted.operext
from inserted
inner join oc on oc.olcodigo=inserted.olcodigo
where inserted.process=1 and (inserted.totol2<>0 or inserted.etotol2<>0)
and substring('2'+inserted.restamp,1,25) not in (select ol.restamp from ol where ol.restamp=substring('2'+inserted.restamp,1,25))END  IF (SELECT COUNT(inserted.REstamp) from inserted inner join deleted on deleted.REstamp=inserted.REstamp where inserted.anulado = 1 AND deleted.anulado = 0) > 0  BEGIN UPDATE RE SET RE.arquivadodigital = 0  FROM inserted  INNER JOIN deleted ON deleted.REstamp=inserted.REstamp  INNER JOIN RE ON RE.REstamp=inserted.REstamp  WHERE inserted.anulado=1 AND deleted.anulado=0  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ft_update ON ft WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128)
SELECT @Cinfo = Context_Info()
IF @Cinfo = 0x55555
RETURN
IF EXISTS(SELECT 1 FROM ft2 as tempft2
INNER JOIN inserted ON inserted.ftstamp COLLATE Latin1_General_CI_AI = tempft2.ft2stamp COLLATE Latin1_General_CI_AI
and tempft2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN td ON inserted.ndoc = td.ndoc
WHERE (td.assinadoc = 1 or (td.tipodoc>=1 and td.tipodoc<=4))
AND td.assinaimp = 0
AND td.docsext = 0
AND td.docsimport = 0
AND inserted.fno>0
and td.fechada=0)
BEGIN
DECLARE @ErrorMessage NVARCHAR(2000)
DECLARE @ErrorSeverity INT
DECLARE @ErrorState INT
SET @ErrorSeverity = 16
SET @ErrorState = 1
SELECT TOP 1
@ErrorMessage = 'A assinatura digital no documento ' + rtrim(inserted.nmdoc) + ' n.º ' + cast(inserted.fno as varchar(25)) +  ' está vazia. Por favor contacte o administrador do sistema.'
FROM ft2 as tempft2
INNER JOIN inserted ON inserted.ftstamp COLLATE Latin1_General_CI_AI = tempft2.ft2stamp COLLATE Latin1_General_CI_AI
and tempft2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN td ON inserted.ndoc = td.ndoc
WHERE (td.assinadoc = 1 or (td.tipodoc>=1 and td.tipodoc<=4))
AND td.assinaimp = 0
AND td.docsext = 0
AND td.docsimport = 0
AND inserted.fno>0
and td.fechada=0
RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
ROLLBACK
END

IF update(ndoc) or update(no) OR UPDATE(tipodoc) OR UPDATE(ttiliq) OR UPDATE(ettiliq) OR UPDATE(anulado)
BEGIN

IF (select count(*) from deleted inner join ft2 on ft2.ft2stamp = deleted.ftstamp inner join td on td.ndoc=deleted.ndoc
inner join cl on cl.no=(case when td.lancacli=0 then deleted.no else ft2.c2no end) And cl.estab=(case when td.lancacli=0 then deleted.estab else ft2.c2estab end)
where (deleted.tipodoc=1 or deleted.tipodoc=3) )>0
UPDATE CL SET cl.acmfact=cl.acmfact-deleted.ttiliq,cl.eacmfact=cl.eacmfact-deleted.ettiliq from DELETED
inner join ft2 on ft2.ft2stamp = deleted.ftstamp inner join inserted on inserted.ftstamp = ft2.ft2stamp inner join td on td.ndoc=deleted.ndoc and td.regrd = 0
where cl.no=(case when td.lancacli=0 then deleted.no else ft2.c2no end) and deleted.anulado =0 and inserted.anulado = 0 and (deleted.tipodoc=1 or deleted.tipodoc=3)
IF (select count(*) from inserted inner join ft2 on ft2.ft2stamp = inserted.ftstamp inner join td on td.ndoc=inserted.ndoc and td.regrd = 0
inner join cl on cl.no=(case when td.lancacli=0 then inserted.no else ft2.c2no end) And cl.estab=(case when td.lancacli=0 then inserted.estab else ft2.c2estab end)
where (inserted.tipodoc=1 or inserted.tipodoc=3) )>0
UPDATE CL SET cl.acmfact=cl.acmfact+inserted.ttiliq,cl.eacmfact=cl.eacmfact+inserted.ettiliq from INSERTED
inner join ft2 on ft2.ft2stamp = inserted.ftstamp inner join td on td.ndoc=inserted.ndoc and td.regrd = 0
where inserted.anulado=0 and cl.no=(case when td.lancacli=0 then inserted.no else ft2.c2no end) and (inserted.tipodoc=1 or inserted.tipodoc=3)
END

DELETE from ftcc from inserted where inserted.multi = 0 and ftcc.ftstamp=inserted.ftstamp
IF UPDATE(chdata) OR UPDATE(no) OR UPDATE(nome) OR UPDATE(estab) OR UPDATE(chtotal) OR UPDATE(echtotal) OR UPDATE(chtmoeda)
OR UPDATE(moeda) OR UPDATE(clbanco) OR UPDATE(clcheque) OR UPDATE(fref) OR UPDATE(tptit) OR UPDATE(ndoc) OR UPDATE(multi) OR UPDATE(cheque)
 BEGIN
DELETE from CH from deleted
where ch.ftstamp = deleted.ftstamp and deleted.multi=0 and deleted.cheque=1 and deleted.ftstamp not in (select inserted.ftstamp from inserted where inserted.multi=0 and inserted.cheque=1)
 END
DELETE from CC from deleted
where cc.ftstamp = deleted.ftstamp and deleted.multi=0 and deleted.ftstamp not in (select inserted.ftstamp from inserted where inserted.multi=0)
DELETE from CC from deleted
where cc.ccstamp = substring('R'+deleted.ftstamp,1,25) and deleted.multi=0
IF UPDATE(chdata) OR UPDATE(no) OR UPDATE(nome) OR UPDATE(estab) OR UPDATE(chtotal) OR UPDATE(echtotal) OR UPDATE(chtmoeda)
OR UPDATE(moeda) OR UPDATE(clbanco) OR UPDATE(clcheque) OR UPDATE(fref) OR UPDATE(tptit) OR UPDATE(ndoc) OR UPDATE(multi) OR UPDATE(cheque)
 BEGIN
INSERT INTO CH (chstamp,ftstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,
clbanco,clcheque,fref,tptit,contado,ollocal)
select inserted.ftstamp, inserted.ftstamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,
inserted.chdata, inserted.ousrdata, inserted.ousrdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,
case when td.lancacli=0 then inserted.nome else ft2.c2nome end,
case when td.lancacli=0 then inserted.estab else ft2.c2estab end,
'FT',
case when td.tipodoc=3 then 0 else inserted.chtotal end,case when td.tipodoc=3 then 0 else inserted.echtotal end,
case when td.tipodoc=3 then 0 else inserted.chtmoeda end, inserted.moeda,
inserted.clbanco, inserted.clcheque, inserted.fref, inserted.tptit,
case when td.lancaol = 1 then case when td.altollocal = 1 then ft2.vdcontado else td.vdcontado end else 0 end,
case when td.lancaol = 1 then case when td.altollocal = 1 then ft2.vdollocal else td.vdollocal end else SPACE(50) end
from inserted inner join td on inserted.ndoc=td.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
where inserted.multi=0 and inserted.cheque=1
and inserted.ftstamp not in (select ch.ftstamp from ch where ch.ftstamp = inserted.ftstamp)
IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=1 and inserted.multi=0)>0
INSERT INTO CC (tpstamp,tpdesc,ccstamp,ftstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
datalc,no,nome,cm,cmdesc,origem,cobranca,cambiofixo,deb,edeb,cred,ecred,
debm,credm,
credf,ecredf,credfm,
ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,
eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,
ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9, reexgiva,
datasup12, formapag,situacao,
clbanco,clcheque,
moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,segmento,tipo,pais,estab,vendedor,vendnm
,eivacativado, ivacativado, mivacativado
, valch,evalch,virs,evirs)
select inserted.tpstamp, inserted.tpdesc, inserted.ftstamp, inserted.ftstamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,
inserted.fdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.cmcc,td.cmccn,'FT',
inserted.cobranca, inserted.cambiofixo,case when td.tipodoc=3 then 0 else inserted.total end,case when td.tipodoc=3 then 0 else inserted.etotal end,
case when td.tipodoc=3 then inserted.total*-1 else 0 end,case when td.tipodoc=3 then inserted.etotal*-1 else 0 end,
case when td.tipodoc=3 then 0 else inserted.totalmoeda end,case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end,
case when inserted.debreg>=case when td.tipodoc=3 then inserted.total*-1 else 0 end then case when td.tipodoc=3 then inserted.total*-1 else 0 end else inserted.debreg end,
case when inserted.edebreg>=case when td.tipodoc=3 then inserted.etotal*-1 else 0 end then case when td.tipodoc=3 then inserted.etotal*-1 else 0 end else inserted.edebreg end,
case when inserted.debregm>=case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end then case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end else inserted.debregm end,
inserted.ivav1, inserted.ivav2, inserted.ivav3, inserted.ivav4, inserted.ivav5, inserted.ivav6, inserted.ivav7, inserted.ivav8, inserted.ivav9,
inserted.eivav1, inserted.eivav2, inserted.eivav3, inserted.eivav4, inserted.eivav5, inserted.eivav6, inserted.eivav7, inserted.eivav8, inserted.eivav9,
inserted.ivatx1, inserted.ivatx2, inserted.ivatx3, inserted.ivatx4, inserted.ivatx5, inserted.ivatx6, inserted.ivatx7, inserted.ivatx8, inserted.ivatx9,
ft2.reexgiva,
dateadd(Month,13, inserted.fdata-DAY(inserted.fdata)),
isnull(ft2.formapag,'1'),'1',
inserted.clbanco, inserted.clcheque,
inserted.moeda, inserted.fno, inserted.pdata, inserted.fref, inserted.intid, inserted.cobrador, inserted.rota, inserted.ccusto, inserted.ncusto, inserted.zona, inserted.segmento, inserted.tipo,
case when td.lancacli=0 then inserted.pais else ft2.c2pais end,case when td.lancacli=0 then inserted.estab else ft2.c2estab end, inserted.vendedor, inserted.vendnm
, isnull(ft3.eivacativado, 0), isnull(ft3.ivacativado, 0)
,isnull(ft3.mivacativado, 0)
, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.chtotal else 0 end
, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.echtotal else 0 end
, inserted.virs, inserted.evirs
 from inserted inner join td on inserted.ndoc=td.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
 inner join ft3 on ft3.ft3stamp = inserted.ftstamp
 where td.lancacc=1 and inserted.multi = 0
and inserted.ftstamp not in (select cc.ccstamp from cc where cc.ccstamp = inserted.ftstamp)
 END
IF UPDATE(total) OR UPDATE(etotal) OR UPDATE(totalmoeda) OR UPDATE(virs) OR UPDATE(evirs) OR UPDATE(ndoc)
UPDATE CC set  debf=case when td.tipodoc=3 then 0 else inserted.total end, edebf=case when td.tipodoc=3 then 0 else inserted.etotal end, debfm=case when td.tipodoc=3 then 0 else inserted.totalmoeda end
, credf=case when td.tipodoc<>3 then 0 else inserted.total*-1 end
, ecredf=case when td.tipodoc<>3 then 0 else inserted.etotal*-1 end
, credfm=case when td.tipodoc<>3 then 0 else inserted.totalmoeda*-1 end
, eivacatreg = case when isnull(ft3.erdivacativado, 0) != 0 then isnull(ft3.eivacativado - ft3.erdivacativado, 0) else isnull(ft3.eivacativado, 0) end
, ivacatreg = case when isnull(ft3.rdivacativado, 0) != 0 then isnull(ft3.ivacativado - ft3.rdivacativado, 0) else isnull(ft3.ivacativado, 0) end
, mivacatreg = case when isnull(ft3.mrdivacativado, 0) != 0 then isnull(ft3.mivacativado - ft3.mrdivacativado, 0) else isnull(ft3.mivacativado, 0) end
, virsreg=inserted.virs
, evirsreg=inserted.evirs
 from inserted inner join td on td.ndoc=inserted.ndoc inner join cc on cc.ftstamp=inserted.ftstamp
 inner join ft3 on ft3.ft3stamp = inserted.ftstamp
 where td.lancacc=1 and td.ccautoreg=1
INSERT INTO CC (tpstamp,tpdesc,ccstamp,ftstamp,
usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,
datalc,no,nome,cm,cmdesc,origem,cobranca,cambiofixo,
cred,ecred,credm,credf,ecredf,credfm,deb,edeb,debm,debf,edebf,debfm,
ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,
eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,
ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9, reexgiva,
datasup12, formapag,situacao,
clbanco,clcheque,
moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,segmento,tipo,pais,estab,vendedor,vendnm
,eivacativado, ivacativado, mivacativado
,eivacatreg, ivacatreg, mivacatreg
, valch,evalch)
select inserted.tpstamp, inserted.tpdesc,substring('R'+inserted.ftstamp,1,25), inserted.ftstamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,
inserted.fdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.cmccautoreg,td.cmccautoregn,'FT',
inserted.cobranca, inserted.cambiofixo,
case when td.tipodoc=3 then 0 else inserted.total-inserted.rdtotal end,case when td.tipodoc=3 then 0 else inserted.etotal-inserted.erdtotal end,case when td.tipodoc=3 then 0 else inserted.totalmoeda-inserted.rdtotalm end,
case when td.tipodoc=3 then 0 else inserted.total-inserted.rdtotal end,case when td.tipodoc=3 then 0 else inserted.etotal-inserted.erdtotal end,case when td.tipodoc=3 then 0 else inserted.totalmoeda-inserted.rdtotalm end,
case when td.tipodoc<>3 then 0 else (inserted.total-inserted.rdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.etotal-inserted.erdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.totalmoeda-inserted.rdtotalm)*-1 end,
case when td.tipodoc<>3 then 0 else (inserted.total-inserted.rdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.etotal-inserted.erdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.totalmoeda-inserted.rdtotalm)*-1 end,
inserted.ivav1, inserted.ivav2, inserted.ivav3, inserted.ivav4, inserted.ivav5, inserted.ivav6, inserted.ivav7, inserted.ivav8, inserted.ivav9,
inserted.eivav1, inserted.eivav2, inserted.eivav3, inserted.eivav4, inserted.eivav5, inserted.eivav6, inserted.eivav7, inserted.eivav8, inserted.eivav9,
inserted.ivatx1, inserted.ivatx2, inserted.ivatx3, inserted.ivatx4, inserted.ivatx5, inserted.ivatx6, inserted.ivatx7, inserted.ivatx8, inserted.ivatx9,
ft2.reexgiva,
dateadd(Month,13, inserted.fdata-DAY(inserted.fdata)),
isnull(ft2.formapag,'1'), '1'
, inserted.clbanco, inserted.clcheque
, inserted.moeda, inserted.fno, inserted.pdata, inserted.fref, inserted.intid, inserted.cobrador, inserted.rota, inserted.ccusto, inserted.ncusto, inserted.zona, inserted.segmento, inserted.tipo
, case when td.lancacli=0 then inserted.pais else ft2.c2pais end, case when td.lancacli=0 then inserted.estab else ft2.c2no end, inserted.vendedor, inserted.vendnm
, case when td.tipodoc<>3 then case when isnull(ft3.erdivacativado, 0) != 0 then isnull((ft3.eivacativado - ft3.erdivacativado)*-1, 0) else isnull(ft3.eivacativado*-1, 0) end  else case when isnull(ft3.erdivacativado, 0) != 0 then isnull((ft3.eivacativado - ft3.erdivacativado), 0) else isnull(ft3.eivacativado, 0) end end
, case when td.tipodoc<>3 then case when isnull(ft3.rdivacativado, 0) != 0 then isnull((ft3.ivacativado - ft3.rdivacativado)*-1, 0) else isnull(ft3.ivacativado*-1, 0) end  else case when isnull(ft3.rdivacativado, 0) != 0 then isnull((ft3.ivacativado - ft3.rdivacativado), 0) else isnull(ft3.ivacativado, 0) end end
, case when td.tipodoc<>3 then case when isnull(ft3.mrdivacativado, 0) != 0 then isnull((ft3.mivacativado - ft3.mrdivacativado)*-1, 0) else isnull(ft3.mivacativado*-1, 0) end  else case when isnull(ft3.mrdivacativado, 0) != 0 then isnull((ft3.mivacativado - ft3.mrdivacativado), 0) else isnull(ft3.mivacativado, 0) end end
, case when isnull(ft3.erdivacativado, 0) != 0 then isnull(ft3.eivacativado - ft3.erdivacativado, 0) else isnull(ft3.eivacativado, 0) end
, case when isnull(ft3.rdivacativado, 0) != 0 then isnull(ft3.ivacativado - ft3.rdivacativado, 0) else isnull(ft3.ivacativado, 0) end
, case when isnull(ft3.mrdivacativado, 0) != 0 then isnull(ft3.mivacativado - ft3.mrdivacativado, 0) else isnull(ft3.mivacativado, 0) end
, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.chtotal else 0 end
, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.echtotal else 0 end
 from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
 inner join ft3 on ft3.ft3stamp = inserted.ftstamp
 where td.lancacc=1 and td.ccautoreg=1 and inserted.multi=0
IF UPDATE(chdata) OR UPDATE(no) OR UPDATE(nome) OR UPDATE(estab) OR UPDATE(chtotal) OR UPDATE(echtotal) OR UPDATE(chtmoeda)
OR UPDATE(moeda) OR UPDATE(clbanco) OR UPDATE(clcheque) OR UPDATE(fref) OR UPDATE(tptit) OR UPDATE(ndoc) OR UPDATE(multi) OR UPDATE(cheque)
 BEGIN
UPDATE CH
set ch.usrdata=inserted.usrdata,ch.usrhora=inserted.usrhora,ch.usrinis=inserted.usrinis,
ch.data=inserted.chdata,ch.no=case when td.lancacli=0 then inserted.no else ft2.c2no end,
ch.nome=case when td.lancacli=0 then inserted.nome else ft2.c2nome end,
ch.estab=case when td.lancacli=0 then inserted.estab else ft2.c2estab end,
ch.origem='FT',
ch.valor=case when td.tipodoc=3 then 0 else inserted.chtotal end,ch.evalor=case when td.tipodoc=3 then 0 else inserted.echtotal end,
ch.valorm=case when td.tipodoc=3 then 0 else inserted.chtmoeda end,ch.moeda=inserted.moeda,
ch.clbanco=inserted.clbanco,ch.clcheque=inserted.clcheque,ch.fref=inserted.fref,ch.tptit=inserted.tptit
, ch.contado = case when td.lancaol = 1 then case when td.altollocal = 1 then ft2.vdcontado else td.vdcontado end else 0 end,
ch.ollocal = case when td.lancaol = 1 then case when td.altollocal = 1 then ft2.vdollocal else td.vdollocal end else SPACE(50) end
from inserted inner join td on inserted.ndoc=td.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
where inserted.ftstamp=ch.ftstamp and inserted.multi=0 and inserted.cheque=1 and inserted.ftstamp IN (select deleted.ftstamp from deleted)
DELETE FROM CH FROM inserted where inserted.cheque=0 and inserted.ftstamp in (select ch.chstamp from ch where ch.chstamp=inserted.ftstamp) and inserted.ftstamp=ch.chstamp
 END
IF UPDATE(usrdata) OR UPDATE(usrhora) OR UPDATE(usrinis) OR UPDATE(tpdesc) OR UPDATE(fdata) OR UPDATE(no) OR UPDATE(nome) OR UPDATE(cobranca) OR UPDATE(cambiofixo)
OR UPDATE(moeda) OR UPDATE(fno) OR UPDATE(pdata) OR UPDATE(fref) OR UPDATE(intid) OR UPDATE(cobrador) OR UPDATE(rota) OR UPDATE(ccusto) OR UPDATE(ncusto)
OR UPDATE(zona) OR UPDATE(segmento) OR UPDATE(tipo) OR UPDATE(pais) OR UPDATE(estab) OR UPDATE(vendedor) OR UPDATE(vendnm)
OR UPDATE(ivav1) OR UPDATE(ivav2) OR UPDATE(ivav3) OR UPDATE(ivav4) OR UPDATE(ivav5) OR UPDATE(ivav6) OR UPDATE(ivav7) OR UPDATE(ivav8) OR UPDATE(ivav9)
OR UPDATE(eivav1) OR UPDATE(eivav2) OR UPDATE(eivav3) OR UPDATE(eivav4) OR UPDATE(eivav5) OR UPDATE(eivav6) OR UPDATE(eivav7) OR UPDATE(eivav8) OR UPDATE(eivav9)
OR UPDATE(ivatx1) OR UPDATE(ivatx2) OR UPDATE(ivatx3) OR UPDATE(ivatx4) OR UPDATE(ivatx5) OR UPDATE(ivatx6) OR UPDATE(ivatx7) OR UPDATE(ivatx8) OR UPDATE(ivatx9)
OR UPDATE(total) OR UPDATE(etotal) OR UPDATE(totalmoeda) OR UPDATE(clbanco) OR UPDATE(clcheque) OR UPDATE(debreg) OR UPDATE(debregm) OR UPDATE(cheque) OR UPDATE(virs)
OR UPDATE(evirs) OR UPDATE(ndoc) OR UPDATE(multi) OR UPDATE(echtotal) OR UPDATE(chtotal)
 BEGIN
UPDATE CC
set
cc.credf=(case when deleted.debreg>=cc.credf then 0 else cc.credf-deleted.debreg end),
cc.ecredf=(case when deleted.edebreg>=cc.ecredf then 0 else cc.ecredf-deleted.edebreg end),
cc.credfm=(case when deleted.debregm>=cc.credfm then 0 else cc.credfm-deleted.debregm end),
cc.valch=cc.valch-case when deleted.cheque=1 and td.tipodoc<>3 then deleted.chtotal else 0 end,
cc.evalch=cc.evalch-case when deleted.cheque=1 and td.tipodoc<>3 then deleted.echtotal else 0 end
from deleted inner join td on deleted.ndoc=td.ndoc where cc.ccstamp<>substring('R'+deleted.ftstamp,1,25) and deleted.ftstamp=cc.ftstamp and td.lancacc=1 and deleted.multi=0
UPDATE CC
set cc.usrdata=inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis,
cc.tpstamp=inserted.tpstamp,cc.tpdesc=inserted.tpdesc,
cc.datalc=inserted.fdata,cc.no=case when td.lancacli=0 then inserted.no else ft2.c2no end,cc.nome=case when td.lancacli=0 then inserted.nome else ft2.c2nome end,cc.cm=td.cmcc,cc.cmdesc=td.cmccn,cc.cobranca=inserted.cobranca,
cc.cambiofixo=inserted.cambiofixo,
cc.moeda=inserted.moeda,cc.nrdoc=inserted.fno,cc.dataven=inserted.pdata,cc.fref=inserted.fref,cc.intid=inserted.intid,cc.cobrador=inserted.cobrador,cc.rota=inserted.rota,cc.ccusto=inserted.ccusto,cc.ncusto=inserted.ncusto,cc.zona=inserted.zona,
cc.segmento=inserted.segmento,cc.tipo=inserted.tipo,cc.pais=case when td.lancacli=0 then inserted.pais else ft2.c2pais end,cc.estab=case when td.lancacli=0 then inserted.estab else ft2.c2estab end,
cc.vendedor=inserted.vendedor,cc.vendnm=inserted.vendnm,
cc.ivav1=inserted.ivav1,cc.ivav2=inserted.ivav2,cc.ivav3=inserted.ivav3,cc.ivav4=inserted.ivav4,cc.ivav5=inserted.ivav5,cc.ivav6=inserted.ivav6,cc.ivav7=inserted.ivav7,cc.ivav8=inserted.ivav8,cc.ivav9=inserted.ivav9,
cc.eivav1=inserted.eivav1,cc.eivav2=inserted.eivav2,cc.eivav3=inserted.eivav3,cc.eivav4=inserted.eivav4,cc.eivav5=inserted.eivav5,cc.eivav6=inserted.eivav6,cc.eivav7=inserted.eivav7,cc.eivav8=inserted.eivav8,cc.eivav9=inserted.eivav9,
cc.ivatx1=inserted.ivatx1,cc.ivatx2=inserted.ivatx2,cc.ivatx3=inserted.ivatx3,cc.ivatx4=inserted.ivatx4,cc.ivatx5=inserted.ivatx5,cc.ivatx6=inserted.ivatx6,cc.ivatx7=inserted.ivatx7,cc.ivatx8=inserted.ivatx8,cc.ivatx9=inserted.ivatx9,
cc.reexgiva=td.reexgiva,
cc.datasup12 =dateadd(Month,13, inserted.fdata-DAY(inserted.fdata)),
cc.formapag=ft2.formapag,
cc.deb=case when td.tipodoc=3 then 0 else inserted.total end,cc.edeb=case when td.tipodoc=3 then 0 else inserted.etotal end,
cc.cred=case when td.tipodoc=3 then inserted.total*-1 else 0 end,cc.ecred=case when td.tipodoc=3 then inserted.etotal*-1 else 0 end,
cc.debm=case when td.tipodoc=3 then 0 else inserted.totalmoeda end,
cc.credm=case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end,
cc.clbanco =inserted.clbanco,cc.clcheque=inserted.clcheque,
cc.credf =(case when cc.credf+inserted.debreg>=(case when td.tipodoc=3 then inserted.total*-1 else 0 end) then (case when td.tipodoc=3 then inserted.total*-1 else 0 end) else cc.credf+inserted.debreg end),
cc.ecredf =(case when cc.ecredf+inserted.edebreg>=(case when td.tipodoc=3 then inserted.etotal*-1 else 0 end) then (case when td.tipodoc=3 then inserted.etotal*-1 else 0 end) else cc.ecredf+inserted.edebreg end),
cc.credfm =(case when cc.credfm+inserted.debregm>=(case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end) then (case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end) else cc.credfm+inserted.debregm end),
cc.valch =cc.valch+case when inserted.cheque=1 and td.tipodoc<>3 then inserted.chtotal else 0 end,
cc.evalch =cc.evalch+case when inserted.cheque=1 and td.tipodoc<>3 then inserted.echtotal else 0 end,
cc.eivacativado = isnull(ft3.eivacativado, 0),
cc.ivacativado = isnull(ft3.ivacativado, 0),
cc.mivacativado = isnull(ft3.mivacativado, 0),
cc.virs=inserted.virs,cc.evirs=inserted.evirs
 from inserted inner join td on inserted.ndoc=td.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
 inner join ft3 on ft3.ft3stamp = inserted.ftstamp
 where cc.ccstamp<>substring('R'+inserted.ftstamp,1,25) and inserted.ftstamp=cc.ftstamp and td.lancacc=1 and inserted.multi=0 and
inserted.ftstamp IN (select deleted.ftstamp from deleted) and cc.ccstamp <> substring('E'+inserted.ftstamp,1,25)
 END
IF UPDATE(usrdata) OR UPDATE(usrhora) OR UPDATE(usrinis) OR UPDATE(ousrdata) OR UPDATE(ousrhora) OR UPDATE(ousrinis)
OR UPDATE(nmdoc) OR UPDATE(fno) OR UPDATE(fdata) OR UPDATE(total) OR UPDATE(rdtotal) OR UPDATE(virs) OR UPDATE(etotal) OR UPDATE(erdtotal)
OR UPDATE(evirs) OR UPDATE(valorm2) OR UPDATE(nome) OR UPDATE(fref) OR UPDATE(intid) OR UPDATE(ccusto) OR UPDATE(ncusto) OR UPDATE(site)
OR UPDATE(pnome) OR UPDATE(pno) OR UPDATE(cxstamp) OR UPDATE(ssstamp) OR UPDATE(ssusername) OR UPDATE(chtotal) OR UPDATE(echtotal) OR UPDATE(chtmoeda) OR UPDATE(ndoc)
UPDATE OL SET
ol.olstamp=inserted.ftstamp,ol.ftstamp=inserted.ftstamp,
ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,
ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='FT',
ol.documento=inserted.nmdoc,ol.cheque=convert(char(15), inserted.fno),
ol.data=inserted.fdata,ol.dvalor=inserted.fdata
,ol.entr=inserted.total-inserted.rdtotal-inserted.virs
,ol.eentr=inserted.etotal-inserted.erdtotal-inserted.evirs
,ol.entrm=inserted.valorm2,ol.descricao=case when td.lancacli=0 then inserted.nome else ft2.c2nome end,ol.olcodigo=td.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo,
ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,
ol.ollocal = case when td.altollocal = 1 then ft2.vdollocal else td.vdollocal end,
ol.contado = case when td.altollocal = 1 then ft2.vdcontado else td.vdcontado end,
ol.local = case when td.altollocal = 1 then ft2.vdlocal else td.vdlocal end,
ol.moeda = case when td.altollocal = 1 then ft2.olmoeda else td.olmoeda end,
ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp,
ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=isnull(ft2.operext, 0),
 ol.acerto=ft2.acerto, ol.eacerto=ft2.eacerto, ol.macerto=ft2.macerto,
 ol.vdinheiro=case when ft2.parcial<>0 then ft2.parcial else case when ft2.eparcial=0 then ft2.vdinheiro else 0 end end,
 ol.evdinheiro=case when ft2.eparcial<>0 then ft2.eparcial else case when ft2.parcial=0 then ft2.evdinheiro else 0 end end,
 ol.mvdinheiro=ft2.mvdinheiro,
 ol.chtotal=inserted.chtotal, ol.echtotal=inserted.echtotal, ol.chtmoeda=inserted.chtmoeda,
 ol.modop1=ft2.modop1, ol.epaga1=ft2.epaga1, ol.paga1=ft2.paga1, ol.mpaga1=ft2.mpaga1,
 ol.modop2=ft2.modop2, ol.epaga2=ft2.epaga2, ol.paga2=ft2.paga2, ol.mpaga2=ft2.mpaga2,
 ol.modop3=ft2.modop3, ol.epaga3=ft2.epaga3, ol.paga3=ft2.paga3, ol.mpaga3=ft2.mpaga3,
 ol.modop4=ft2.modop4, ol.epaga4=ft2.epaga4, ol.paga4=ft2.paga4, ol.mpaga4=ft2.mpaga4,
 ol.modop5=ft2.modop5, ol.epaga5=ft2.epaga5, ol.paga5=ft2.paga5, ol.mpaga5=ft2.mpaga5,
 ol.modop6=ft2.modop6, ol.epaga6=ft2.epaga6, ol.paga6=ft2.paga6, ol.mpaga6=ft2.mpaga6
 from inserted inner join td on td.ndoc=inserted.ndoc inner join oc on oc.olcodigo=td.olcodigo
 inner join ft2 on ft2.ft2stamp = inserted.ftstamp where td.lancaol=1 and ol.ftstamp=inserted.ftstamp
IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancaol=1)>0
INSERT INTO OL (olstamp,ftstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,origem,
documento,cheque,
data,dvalor,entr,eentr,entrm,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,
site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext,
ollocal,contado,local,moeda,
 acerto, eacerto, macerto, vdinheiro, evdinheiro, mvdinheiro,
 chtotal, echtotal, chtmoeda,
 modop1, epaga1, paga1, mpaga1, modop2, epaga2, paga2, mpaga2,
 modop3, epaga3, paga3, mpaga3, modop4, epaga4, paga4, mpaga4,
 modop5, epaga5, paga5, mpaga5, modop6, epaga6, paga6, mpaga6 )
select inserted.ftstamp, inserted.ftstamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,'FT',
inserted.nmdoc,convert(char(15), inserted.fno), inserted.fdata, inserted.fdata
, inserted.total-inserted.rdtotal-inserted.virs
, inserted.etotal-inserted.erdtotal-inserted.evirs
, inserted.valorm2,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.olcodigo,oc.olgrupo,oc.olsgrupo,
inserted.fref, inserted.intid, inserted.ccusto, inserted.ncusto,
inserted.site, inserted.pnome, inserted.pno, inserted.cxstamp, inserted.cxusername,
inserted.ssstamp, inserted.ssusername,isnull(ft2.operext, 0),
case when td.altollocal = 1 then ft2.vdollocal else td.vdollocal end,
case when td.altollocal = 1 then ft2.vdcontado else td.vdcontado end,
case when td.altollocal = 1 then ft2.vdlocal else td.vdlocal end,
case when td.altollocal = 1 then ft2.olmoeda else td.olmoeda end,
ft2.acerto, ft2.eacerto, ft2.macerto,
case when ft2.parcial<>0 then ft2.parcial else case when ft2.eparcial=0 then ft2.vdinheiro else 0 end end,
case when ft2.eparcial<>0 then ft2.eparcial else case when ft2.parcial=0 then ft2.evdinheiro else 0 end end,
ft2.mvdinheiro,
inserted.chtotal, inserted.echtotal, inserted.chtmoeda,
ft2.modop1, ft2.epaga1, ft2.paga1, ft2.mpaga1, ft2.modop2, ft2.epaga2, ft2.paga2, ft2.mpaga2,
ft2.modop3, ft2.epaga3, ft2.paga3, ft2.mpaga3, ft2.modop4, ft2.epaga4, ft2.paga4, ft2.mpaga4,
ft2.modop5, ft2.epaga5, ft2.paga5, ft2.mpaga5, ft2.modop6, ft2.epaga6, ft2.paga6, ft2.mpaga6
from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
inner join oc on oc.olcodigo=td.olcodigo where td.lancaol=1
and inserted.ftstamp not in (select ol.olstamp from ol where ol.olstamp = inserted.ftstamp)
if (select count(*) from deleted where deleted.lrstamp<>'')>0
UPDATE LR SET lr.edebi=lr.edebi-deleted.total,
lr.eedebi=lr.eedebi-deleted.etotal from deleted where lr.lrstamp=deleted.lrstamp
if (select count(*) from inserted where inserted.lrstamp<>'')>0
UPDATE LR SET lr.edebi=lr.edebi+inserted.total,
lr.eedebi=lr.eedebi+inserted.etotal from inserted where lr.lrstamp=inserted.lrstamp and inserted.anulado =0
if (select count(*) from deleted where deleted.lpstamp<>'')>0
UPDATE LP SET lp.edebi=lp.edebi-deleted.total,
lp.eedebi=lp.eedebi-deleted.etotal from deleted where lp.lpstamp=deleted.lpstamp
if (select count(*) from inserted where inserted.lpstamp<>'')>0
UPDATE LP SET lp.edebi=lp.edebi+inserted.total,
lp.eedebi=lp.eedebi+inserted.etotal from inserted where lp.lpstamp=inserted.lpstamp and inserted.anulado =0
IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=0 and inserted.rdtotal<>0 and inserted.tipodoc=1)=0
and (select count(*) from deleted inner join td on td.ndoc=deleted.ndoc where td.lancacc=0 and deleted.rdtotal<>0 and deleted.tipodoc=1)>0
DELETE from CC from deleted
where cc.ftstamp = deleted.ftstamp and deleted.multi=0
IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=0 and inserted.rdtotal<>0 and inserted.tipodoc=1)>0
UPDATE CC  set cc.usrdata=inserted.usrdata,cc.usrhora=inserted.usrhora,cc.usrinis=inserted.usrinis,
cc.datalc=inserted.fdata,cc.no=case when td.lancacli=0 then inserted.no else ft2.c2no end,cc.nome=case when td.lancacli=0 then inserted.nome else ft2.c2nome end,cc.cm=td.cmcc,cc.cmdesc=td.cmccn,
cc.cobranca=inserted.cobranca,cc.cambiofixo=inserted.cambiofixo,cc.deb=inserted.rdtotal,cc.edeb=inserted.erdtotal,
cc.debm=inserted.rdtotalm,
cc.moeda=inserted.moeda,cc.nrdoc=inserted.fno,cc.dataven=inserted.pdata,cc.fref=inserted.fref,cc.intid=inserted.intid,
cc.cobrador=inserted.cobrador,cc.rota=inserted.rota,cc.ccusto=inserted.ccusto,cc.ncusto=inserted.ncusto,cc.zona=inserted.zona,
cc.segmento=inserted.segmento,cc.tipo=inserted.tipo,cc.pais=case when td.lancacli=0 then inserted.pais else ft2.c2pais end,cc.estab=case when td.lancacli=0 then inserted.estab else ft2.c2estab end,
cc.vendedor=inserted.vendedor,cc.vendnm=inserted.vendnm
from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp where td.lancacc=0 and inserted.multi=0 and inserted.rdtotal<>0 and inserted.tipodoc=1 and
inserted.ftstamp IN (select deleted.ftstamp from deleted) and inserted.ftstamp=cc.ftstamp
IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=0 and inserted.rdtotal<>0 and inserted.tipodoc=1)>0
INSERT INTO CC (ccstamp,ftstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
datalc,no,nome,cm,cmdesc,origem,cobranca,cambiofixo,deb,edeb, debm,
moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,segmento,tipo,pais,estab,vendedor,vendnm
)
select inserted.ftstamp, inserted.ftstamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,
inserted.fdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.cmcc,td.cmccn,'FT',
inserted.cobranca, inserted.cambiofixo, inserted.rdtotal, inserted.erdtotal,
inserted.rdtotalm,
inserted.moeda, inserted.fno, inserted.pdata, inserted.fref, inserted.intid, inserted.cobrador, inserted.rota, inserted.ccusto, inserted.ncusto, inserted.zona, inserted.segmento, inserted.tipo,
case when td.lancacli=0 then inserted.pais else ft2.c2pais end,case when td.lancacli=0 then inserted.estab else ft2.c2estab end, inserted.vendedor, inserted.vendnm
 from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
 where td.lancacc=0 and inserted.multi=0 and inserted.rdtotal<>0 and inserted.tipodoc=1 and
 not exists (select ccstamp from cc (nolock) where cc.ftstamp=inserted.ftstamp)IF update(anulado) or (select COUNT(*) from inserted where inserted.anulado = 1) > 0
BEGIN DELETE FROM SL FROM inserted INNER JOIN fi (NOLOCK) ON inserted.ftstamp = fi.ftstamp WHERE inserted.anulado =1 and sl.fistamp=fi.fistamp AND fi.fistamp<>''
DELETE FROM SV FROM inserted INNER JOIN fi (NOLOCK) ON inserted.ftstamp = fi.ftstamp WHERE inserted.anulado =1 and sv.fistamp=fi.fistamp and fi.fistamp<>''

update BI
set bi.fno = 0
, bi.adoc = ''
, bi.nmdoc = ''
, bi.oftstamp = ''
, bi.ndoc = 0
, bi.qtt2 = bi.qtt2 -(select sum(fi.qtt *(case when fi.tipodoc=3 then -1 else 1 end))
from inserted
inner join fi (nolock) on inserted.ftstamp = fi.ftstamp
where inserted.anulado =1 and fi.bistamp <> '' and fi.bistamp = bi.bistamp)
from bi
inner join fi (nolock) on fi.bistamp =bi.bistamp
inner join fi2(nolock) on fi.fistamp = fi2.fi2stamp
inner join inserted on inserted.ftstamp =fi.ftstamp
inner join deleted on deleted.ftstamp =fi.ftstamp
where inserted.anulado =1
and deleted.anulado = 0 and fi.bistamp<>'' and fi2.noupdqtt2ori=0

UPDATE BI
set bi.partes2 = bi.partes2 -(select SUM(fi.partes)
from fi
inner join inserted on inserted.ftstamp = fi.ftstamp
where inserted.anulado = 1 and fi.bistamp <> '' and fi.bistamp = bi.bistamp)
from bi
inner join fi (nolock) on fi.bistamp = bi.bistamp
inner join inserted on inserted.ftstamp = fi.ftstamp
inner join ts (nolock) on ts.ndos = bi.ndos
inner join deleted on deleted.ftstamp=fi.ftstamp
where inserted.anulado = 1
and deleted.anulado = 0 and fi.bistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1)

UPDATE FTREG
set ftreg.vciva =ftreg.vciva - (select round(sum(case when fi.ivaincl=1 then ((fi.tiliquido)*-1)*(1-ftreg.fin/100)
else -((fi.tiliquido-round((fi.tiliquido*ftreg.fin/100), 0))*(1+fi.iva/100)) end), 0)
from fi
inner join inserted on inserted.ftstamp = fi.ftstamp
inner join deleted on deleted.ftstamp=fi.ftstamp
inner join ftreg on fi.ftregstamp=ftreg.ftregstamp
where inserted.anulado =1 and deleted.anulado =0)

, ftreg.vmoeda =ftreg.vmoeda - (select round(sum(case when fi.ivaincl=1 then ((fi.tmoeda)*-1)*(1-ftreg.fin/100)
else -((fi.tmoeda-round((fi.tmoeda*ftreg.fin/100), 0))*(1+fi.iva/100)) end), 0)
from fi
inner join inserted on inserted.ftstamp = fi.ftstamp
inner join deleted on deleted.ftstamp=fi.ftstamp
inner join ftreg on fi.ftregstamp=ftreg.ftregstamp
where inserted.anulado =1 and deleted.anulado =0)

, ftreg.evciva =ftreg.evciva - (select round(sum(case when fi.ivaincl=1 then ((fi.etiliquido)*-1)*(1-ftreg.fin/100)
else -((fi.etiliquido-round((fi.etiliquido*ftreg.fin/100), 0))*(1+fi.iva/100)) end), 0)
from fi
inner join inserted on inserted.ftstamp = fi.ftstamp
inner join deleted on deleted.ftstamp=fi.ftstamp
inner join ftreg on fi.ftregstamp=ftreg.ftregstamp where inserted.anulado =1 and deleted.anulado =0)
, ftreg.ultdoc=''
, ftreg.fin = inserted.fin
from ftreg
inner join fi (nolock) on ftreg.ftregstamp=fi.ftregstamp
inner join inserted on inserted.ftstamp=fi.ftstamp
inner join deleted on deleted.ftstamp=fi.ftstamp
where inserted.anulado = 1
and deleted.anulado= 0 and fi.tipodoc=3 and fi.ftregstamp<>space(25)

DELETE FROM PN FROM inserted inner join fi (nolock) on inserted.ftstamp=fi.ftstamp where inserted.anulado =1 and pn.fistamp=fi.fistamp

IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=1)>0
UPDATE CC
SET deb=0, edeb=0, cred=0, ecred=0, debm=0, credm=0
, debf =0, edebf =0, debfm = 0, credf=0, ecredf=0, credfm=0
, ivav1=0, ivav2=0, ivav3=0, ivav4=0, ivav5=0, ivav6=0, ivav7=0, ivav8=0, ivav9=0
, eivav1=0, eivav2=0, eivav3=0,eivav4=0, eivav5=0, eivav6=0, eivav7=0, eivav8=0, eivav9=0
, ivatx1=0, ivatx2=0, ivatx3=0, ivatx4=0, ivatx5=0, ivatx6=0, ivatx7=0, ivatx8=0, ivatx9=0
, valch=0, evalch=0, virs=0,evirs=0
, eivacativado = 0, ivacativado = 0, mivacativado = 0
from inserted
inner join cc (nolock) on inserted.ftstamp = cc.ccstamp
where inserted.anulado = 1

DELETE from CC
from ftrd (nolock)
inner join inserted on ftrd.ftstamp =inserted.ftstamp
where cc.ccstamp ='C'+ftrd.ftrdstamp or cc.ccstamp='D'+ftrd.ftrdstamp and inserted.anulado=1
UPDATE CC SET
cc.difcambio = cc.difcambio - ISNULL((select SUM(ftrd.difcambio) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
										inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0 and ftrd.ccstamp <> '' and ftrd.ccstamp = cc.ccstamp), 0),
cc.edifcambio = cc.edifcambio - ISNULL((select SUM(ftrd.edifcambio) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                                         inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0 and ftrd.ccstamp <> '' and ftrd.ccstamp = cc.ccstamp), 0),
cc.credf=cc.credf - (select SUM(ftrd.vreg) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                      inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0 and ftrd.ccstamp <> '' and ftrd.ccstamp = cc.ccstamp),
cc.ecredf=cc.ecredf - (select SUM(ftrd.evreg) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                        inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0  and ftrd.ccstamp <> '' and ftrd.ccstamp = cc.ccstamp),
cc.credfm=cc.credfm - (select SUM(ftrd.vmreg) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                        inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0  and ftrd.ccstamp <> '' and ftrd.ccstamp = cc.ccstamp)
from cc inner join ftrd (nolock) on ftrd.ccstamp=cc.ccstamp inner join inserted on ftrd.ftstamp=inserted.ftstamp
inner join deleted on deleted.ftstamp=ftrd.ftstamp
where inserted.anulado = 1 and deleted.anulado =0 and ftrd.ccstamp <> ''
update cc set
cc.debf=cc.debf - (select SUM(ftrd.vreg) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                    inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0  and ftrd.ccstamp <> '' and ftrd.ftstamp = cc.ftstamp and ftrd.multi = 0),
cc.edebf=cc.edebf - (select SUM(ftrd.evreg) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                      inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0 and ftrd.ccstamp <> '' and ftrd.ftstamp = cc.ftstamp and ftrd.multi = 0),
cc.debfm=cc.debfm - (select SUM(ftrd.vmreg) from ftrd (nolock) inner join inserted on ftrd.ftstamp = inserted.ftstamp
                      inner join deleted on deleted.ftstamp=ftrd.ftstamp where inserted.anulado=1 and deleted.anulado =0 and ftrd.ccstamp <> '' and ftrd.ftstamp = cc.ftstamp and ftrd.multi = 0)
from cc inner join ft on ft.ftstamp = cc.ftstamp inner join ftrd on ftrd.ftstamp=cc.ftstamp inner join inserted on ftrd.ftstamp= inserted.ftstamp
inner join td on td.ndoc = ft.ndoc
inner join deleted on deleted.ftstamp=ftrd.ftstamp
where inserted.anulado = 1 and deleted.anulado =0 and cc.ccstamp<>'R'+ftrd.ftstamp and ft.multi = 0 and ftrd.multi = 0 and ftrd.ccstamp <> '' and td.ccautoreg = 0
update rd set rd.integrado=
(case isnull((select top 1 ftrd.ftrdstamp from ftrd ftrdori where ftrdori.rdstamp=ftrd.rdstamp),'') when '' then 0 else 1 end)
from ftrd inner join inserted on ftrd.ftstamp=inserted.ftstamp where inserted.anulado = 1 and rd.rdstamp=ftrd.rdstamp

UPDATE CC
set deb = 0, edeb = 0, debf = 0, edebf = 0, debfm = 0, debm = 0
,cred = 0, ecred = 0, credf = 0, ecredf = 0, credfm = 0, credm = 0
,ivav1 = 0, ivav2 = 0, ivav3 = 0, ivav4 = 0, ivav5 = 0, ivav6 = 0, ivav7 = 0, ivav8 = 0, ivav9 = 0
,eivav1 = 0, eivav2 = 0, eivav3 = 0, eivav4 = 0, eivav5 = 0, eivav6 = 0, eivav7 = 0, eivav8 = 0, eivav9 = 0
,ivatx1 = 0, ivatx2 = 0, ivatx3 = 0, ivatx4 = 0, ivatx5 = 0, ivatx6 = 0, ivatx7 = 0, ivatx8 = 0, ivatx9 = 0
, valch = 0, evalch = 0, virs = 0, evirs = 0
, virsreg = 0, evirsreg = 0
, eivacativado = 0, ivacativado = 0, eivacatreg = 0, ivacatreg = 0, mivacativado = 0, mivacatreg = 0
from inserted
inner join td on td.ndoc = inserted.ndoc
inner join cc on cc.ccstamp = 'R' + inserted.ftstamp
where td.lancacc = 1 and td.ccautoreg = 1 and inserted.anulado = 1

DELETE from OL 
from inserted
where ol.ftstamp = inserted.ftstamp and inserted.anulado = 1UPDATE FT3 SET ft3.arquivadodigital = 0  FROM inserted  INNER JOIN deleted ON deleted.ftstamp=inserted.ftstamp  INNER JOIN ft ON ft.ftstamp=inserted.ftstamp  WHERE inserted.anulado=1 AND deleted.anulado=0 UPDATE FT SET ft.facturada = 0, ft.nmdocft = '', ft.fnoft =0   FROM inserted  inner join deleted on deleted.ftstamp=inserted.ftstamp  INNER JOIN fi ON inserted.ftstamp=fi.ftstamp  INNER JOIN ft ON ft.fno=fi.fnoft AND fi.ndocft=ft.ndoc and fi.ftanoft = ft.ftano WHERE inserted.anulado=1 and deleted.anulado=0  UPDATE FI SET fi.ndocft = 0, fi.fnoft =0  FROM inserted  inner join deleted on deleted.ftstamp=inserted.ftstamp  INNER JOIN fi ON inserted.ftstamp=fi.ftstamp  WHERE inserted.anulado=1 and deleted.anulado=0 DELETE FROM cc FROM inserted  INNER JOIN ftcc ON ftcc.ftstamp=inserted.ftstamp WHERE cc.ftstamp = inserted.ftstamp AND inserted.multi=1 AND inserted.anulado =1UPDATE cl SET  cl.acmfact=cl.acmfact-inserted.ttiliq,cl.eacmfact=cl.eacmfact-inserted.ettiliq from inserted inner join ft2 on ft2.ft2stamp = inserted.ftstamp inner join td on td.ndoc=inserted.ndoc and td.regrd = 0 inner join deleted on inserted.ftstamp = deleted.ftstamp where cl.no=(case when td.lancacli=0 then deleted.no else ft2.c2no end) and deleted.anulado = 0 and (deleted.tipodoc=1 or deleted.tipodoc=3) ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fn_insert ON fn WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif ( select count(*) from INSERTED inner join fo on fo.fostamp=inserted.fostamp inner join fo2 on fo2.fo2stamp=inserted.fostamp and fo2.anulado=0 inner join cm1 on fo.doccode=cm1.cm  where cm1.folansl=1  and inserted.ref<>space(18) )>0  INSERT INTO SL (slstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,ref,lote,armazem,qtt,tt,vu,ett,evu,num1,frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,vumoeda,ttmoeda,pcpond,cpoc,valiva,evaliva,epcpond,stns )select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.fref,inserted.fnccusto,fo.ncusto,fo.nome,inserted.codigo,fo.moeda,(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.armazem,inserted.qtt,inserted.sltt,inserted.slvu,inserted.esltt,inserted.eslvu,inserted.num1,fo.no,fo.adoc,cm1.fosl,cm1.fosln,inserted.composto,inserted.cor,inserted.tam,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.slvumoeda,inserted.slvumoeda*inserted.qtt,inserted.slvu,inserted.cpoc,case when inserted.ivaincl=1 then inserted.tiliquido-inserted.tiliquido/(1+inserted.iva/100) else inserted.tiliquido*inserted.iva/100 end,case when inserted.ivaincl=1 then inserted.etiliquido-inserted.etiliquido/(1+inserted.iva/100) else inserted.etiliquido*inserted.iva/100 end,inserted.eslvu,inserted.stns from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm inner join fo2 on fo2.fo2stamp=inserted.fostamp and fo2.anulado=0 where cm1.folansl=1  and inserted.ref<>space(18) if ( select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm  where cm1.folansv=1 and cm1.folansvgr = 0 and inserted.ref<>space(18) )>0  INSERT INTO SV (svstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc,ttmoeda )select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.nome,fo.moeda,(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem,case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0) then inserted.sltt else inserted.tiliquido end,case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0) then inserted.esltt else inserted.etiliquido end,fo.no,fo.adoc,cm1.fosv,cm1.fosvn,inserted.tmoeda from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm  where cm1.folansv=1 and cm1.folansvgr = 0 and inserted.ref<>space(18) if ( select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.bistamp <> '' and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) )>0  INSERT INTO SV (svstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.nome,'PTE ou EURO',(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem, case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0)  then round(inserted.sltt - (SELECT (case when bi.qtt = 0 then bi.debito else bi.ttdeb end) from bi where bi.bistamp = inserted.bistamp), 1) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.pv else inserted.tiliquido / inserted.qtt end) - (SELECT (case when bi.qtt = 0 then bi.debito else bi.ttdeb / bi.qtt end) from bi where bi.bistamp = inserted.bistamp)),1)end,  case when (fo.evalacres > 0 or fo.edespinc > 0 or inserted.evalacres > 0)  then round(inserted.esltt - (SELECT (case when bi.qtt = 0 then bi.edebito else bi.ettdeb end) from bi where bi.bistamp = inserted.bistamp), 2) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.epv else inserted.etiliquido / inserted.qtt end) - (SELECT (case when bi.qtt = 0 then bi.edebito else bi.ettdeb / bi.qtt end) from bi where bi.bistamp = inserted.bistamp)),2)end, fo.no,fo.adoc,cm1.fosv,cm1.fosvn from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) if ( select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.ofnstamp <> '' and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) )>0  INSERT INTO SV (svstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.nome,'PTE ou EURO',(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem, case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0)  then round(inserted.sltt - (SELECT(case when fn.qtt = 0 then fn.pv else fn.tiliquido end) from fn where fn.fnstamp = inserted.ofnstamp), 1) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.pv else inserted.tiliquido / inserted.qtt end) - (SELECT(case when fn.qtt = 0 then fn.pv else fn.tiliquido / fn.qtt end) from fn where fn.fnstamp = inserted.ofnstamp)), 1)end,  case when (fo.evalacres > 0 or fo.edespinc > 0 or inserted.evalacres > 0)  then round(inserted.esltt - (SELECT(case when fn.qtt = 0 then fn.epv else fn.etiliquido end) from fn where fn.fnstamp = inserted.ofnstamp), 2) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.epv else inserted.etiliquido / inserted.qtt end) - (SELECT(case when fn.qtt = 0 then fn.epv else fn.etiliquido / fn.qtt end) from fn where fn.fnstamp = inserted.ofnstamp)), 2)end, fo.no,fo.adoc,cm1.fosv,cm1.fosvn from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) If (select top 1 cm1.fonactqtt2 from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm)=0Beginif ( select count(*) from inserted where inserted.bistamp<>'' )>0  UPDATE BI SET bi.fdata = fo.data,bi.adoc = fo.adoc,bi.ofostamp= fo.fostamp,bi.nmdoc = fo.docnome,bi.ndoc = fo.doccode, bi.qtt2 = bi.qtt2+inserted.qtt  from inserted inner join fo on inserted.fostamp=fo.fostamp where inserted.bistamp=bi.bistampEnd		
			if (select count(*) from inserted where fnofo<>0 and ndocfo<>0) >0 
			Begin
			UPDATE FO2 SET
			fo2.facturada = 1 , 
			fo2.nmdocfo = (select top 1 fo.docnome from fo where fo.fostamp = i.fostamp),
			fo2.fnofo = (select top 1 fo.no from fo where fo.fostamp = i.fostamp),
			fo2.adocfo = (select top 1 fo.adoc from fo where fo.fostamp = i.fostamp) 
			from inserted i
				inner join fo on 
					fo.doccode = i.ndocfo and fo.no = i.fnofo and fo.foano = i.foanofo and fo.adoc = i.adocfo
			where i.ndocfo<>0 
				and fo2.fo2stamp in (select fo2.fo2stamp from fo2
							inner join fo on fo.fostamp = fo2.fo2stamp
								where fo.doccode = i.ndocfo and fo.no = i.fnofo and fo.foano = i.foanofo and fo.adoc = i.adocfo)
			endGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ft_insert ON ft WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF EXISTS(SELECT 1 FROM ft2 as tempft2
INNER JOIN inserted ON inserted.ftstamp COLLATE Latin1_General_CI_AI = tempft2.ft2stamp COLLATE Latin1_General_CI_AI
and tempft2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN td ON inserted.ndoc = td.ndoc
WHERE (td.assinadoc = 1 or (td.tipodoc>=1 and td.tipodoc<=4))
AND td.assinaimp = 0
AND td.docsext = 0
AND td.docsimport = 0
AND inserted.fno>0
and td.fechada=0)
BEGIN
DECLARE @ErrorMessage NVARCHAR(2000)
DECLARE @ErrorSeverity INT
DECLARE @ErrorState INT
SET @ErrorSeverity = 16
SET @ErrorState = 1
SELECT TOP 1
@ErrorMessage = 'A assinatura digital no documento ' + rtrim(inserted.nmdoc) + ' n.º ' + cast(inserted.fno as varchar(25)) +  ' está vazia. Por favor contacte o administrador do sistema.'
FROM ft2 as tempft2
INNER JOIN inserted ON inserted.ftstamp COLLATE Latin1_General_CI_AI = tempft2.ft2stamp COLLATE Latin1_General_CI_AI
and tempft2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
INNER JOIN td ON inserted.ndoc = td.ndoc
WHERE (td.assinadoc = 1 or (td.tipodoc>=1 and td.tipodoc<=4))
AND td.assinaimp = 0
AND td.docsext = 0
AND td.docsimport = 0
AND inserted.fno>0
and td.fechada=0
RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
ROLLBACK
ENDIF (select count(*) from inserted inner join ft2 on ft2.ft2stamp = inserted.ftstamp inner join td on td.ndoc=inserted.ndoc and td.regrd = 0
 inner join cl on cl.no=(case when td.lancacli=0 then inserted.no else ft2.c2no end) And cl.estab=(case when td.lancacli=0 then inserted.estab else ft2.c2estab end)
 where (inserted.tipodoc=1 or inserted.tipodoc=3) )>0
UPDATE CL SET cl.acmfact=cl.acmfact+inserted.ttiliq,cl.eacmfact=cl.eacmfact+inserted.ettiliq from INSERTED
inner join ft2 on ft2.ft2stamp = inserted.ftstamp inner join td on td.ndoc =inserted.ndoc and td.regrd = 0
 where cl.no=(case when td.lancacli=0 then inserted.no else ft2.c2no end) and inserted.anulado = 0 and (inserted.tipodoc=1 or inserted.tipodoc=3)
if (select count(*) from inserted where inserted.multi=0 and inserted.cheque=1)>0
INSERT INTO CH (chstamp,ftstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,
clbanco,clcheque,fref,tptit,contado,ollocal)
select inserted.ftstamp, inserted.ftstamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,
inserted.chdata, inserted.ousrdata, inserted.ousrdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,
case when td.lancacli=0 then inserted.nome else ft2.c2nome end,
case when td.lancacli=0 then inserted.estab else ft2.c2estab end,
'FT',
case when td.tipodoc=3 then 0 else inserted.chtotal end,case when td.tipodoc=3 then 0 else inserted.echtotal end,
case when td.tipodoc=3 then 0 else inserted.chtmoeda end, inserted.moeda,
inserted.clbanco, inserted.clcheque, inserted.fref, inserted.tptit,
case when td.lancaol = 1 then case when td.altollocal = 1 then ft2.vdcontado else td.vdcontado end else 0 end,
case when td.lancaol = 1 then case when td.altollocal = 1 then ft2.vdollocal else td.vdollocal end else SPACE(50) end
from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp
where inserted.multi=0 and inserted.cheque=1

if (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=1 and inserted.multi=0)>0
insert into CC
(tpstamp, tpdesc, ccstamp, ftstamp, usrdata, usrhora, usrinis, ousrdata, ousrhora, ousrinis
, datalc, no, nome, cm, cmdesc, origem, cobranca, cambiofixo
, deb, edeb, cred, ecred, debm, credm, credf, ecredf, credfm
, ivav1, ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9
, ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9, reexgiva
, datasup12, formapag, situacao, clbanco, clcheque, moeda, nrdoc,dataven, fref, intid, cobrador, rota, ccusto, ncusto, zona
, segmento, tipo, pais, estab, vendedor, vendnm, eivacativado, ivacativado, mivacativado
, valch, evalch, virs, evirs  )
select inserted.tpstamp, inserted.tpdesc, inserted.ftstamp, inserted.ftstamp
, inserted.usrdata, inserted.usrhora, inserted.usrinis, inserted.ousrdata, inserted.ousrhora, inserted.ousrinis
, inserted.fdata
, case when td.lancacli=0 then inserted.no else ft2.c2no end
, case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.cmcc,td.cmccn,'FT'
, inserted.cobranca, inserted.cambiofixo
, case when td.tipodoc=3 then 0 else inserted.total end
, case when td.tipodoc=3 then 0 else inserted.etotal end

, case when td.tipodoc=3 then inserted.total*-1 else 0 end
, case when td.tipodoc=3 then inserted.etotal*-1 else 0 end

, case when td.tipodoc=3 then 0 else inserted.totalmoeda end
, case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end

, case when inserted.debreg >= case when td.tipodoc=3 then inserted.total*-1 else 0 end then case when td.tipodoc=3 then inserted.total*-1 else 0 end else inserted.debreg end
, case when inserted.edebreg >= case when td.tipodoc=3 then inserted.etotal*-1 else 0 end then case when td.tipodoc=3 then inserted.etotal*-1 else 0 end else inserted.edebreg end
, case when inserted.debregm >= case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end then case when td.tipodoc=3 then inserted.totalmoeda*-1 else 0 end else inserted.debregm end
, inserted.ivav1, inserted.ivav2, inserted.ivav3, inserted.ivav4, inserted.ivav5, inserted.ivav6, inserted.ivav7, inserted.ivav8, inserted.ivav9
, inserted.eivav1, inserted.eivav2, inserted.eivav3, inserted.eivav4, inserted.eivav5, inserted.eivav6, inserted.eivav7, inserted.eivav8, inserted.eivav9
, inserted.ivatx1, inserted.ivatx2, inserted.ivatx3, inserted.ivatx4, inserted.ivatx5, inserted.ivatx6, inserted.ivatx7, inserted.ivatx8, inserted.ivatx9
, ft2.reexgiva , dateadd(Month,13, inserted.fdata-DAY(inserted.fdata)), ft2.formapag, '1'
, inserted.clbanco, inserted.clcheque
, inserted.moeda, inserted.fno, inserted.pdata, inserted.fref, inserted.intid, inserted.cobrador, inserted.rota, inserted.ccusto
, inserted.ncusto, inserted.zona, inserted.segmento, inserted.tipo
, case when td.lancacli=0 then inserted.pais else ft2.c2pais end
, case when td.lancacli=0 then inserted.estab else ft2.c2estab end
, inserted.vendedor, inserted.vendnm
, isnull(ft3.eivacativado, 0), isnull(ft3.ivacativado, 0), isnull(ft3.mivacativado, 0)
, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.chtotal else 0 end
, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.echtotal else 0 end
, inserted.virs, inserted.evirs
from inserted
inner join td on td.ndoc = inserted.ndoc
inner join ft2 on ft2.ft2stamp = inserted.ftstamp
inner join ft3 on ft3.ft3stamp = inserted.ftstamp
where td.lancacc=1 and inserted.multi=0

update CC
set debf =case when td.tipodoc=3 then 0 else inserted.total end
, edebf =case when td.tipodoc=3 then 0 else inserted.etotal end
, debfm =case when td.tipodoc=3 then 0 else inserted.totalmoeda end
, credf =case when td.tipodoc<>3 then 0 else inserted.total*-1 end
, ecredf =case when td.tipodoc<>3 then 0 else inserted.etotal*-1 end
, credfm =case when td.tipodoc<>3 then 0 else inserted.totalmoeda*-1 end
, eivacativado=  isnull(ft3.eivacativado, 0) 
, ivacativado=  isnull(ft3.ivacativado, 0)
, mivacativado= isnull(ft3.mivacativado, 0)
, eivacatreg = case when isnull(ft3.erdivacativado, 0) = 0 then isnull(ft3.eivacativado, 0) else ft3.erdivacativado end 
, ivacatreg = case when isnull(ft3.rdivacativado, 0) = 0 then isnull(ft3.ivacativado, 0) else ft3.rdivacativado end 
, mivacatreg = case when isnull(ft3.mrdivacativado, 0) = 0 then isnull(ft3.mivacativado, 0) else ft3.mrdivacativado end 
, virsreg =inserted.virs
, evirsreg =inserted.evirs

from inserted
inner join td on td.ndoc=inserted.ndoc
inner join cc on cc.ftstamp=inserted.ftstamp
inner join ft3 on ft3.ft3stamp = inserted.ftstamp
where td.lancacc=1 and td.ccautoreg=1IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=1 and inserted.multi=0)>0INSERT INTO CC (tpstamp,tpdesc,ccstamp,ftstamp,usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,cambiofixo,cred,ecred,credm,credf,ecredf,credfm,deb,edeb,debm,debf,edebf,debfm,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9, reexgiva, datasup12, formapag,situacao, clbanco,clcheque, moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,segmento,tipo,pais,estab,vendedor,vendnm, valch,evalch) select inserted.tpstamp, inserted.tpdesc,substring('R'+inserted.ftstamp,1,25), inserted.ftstamp,inserted.usrdata, inserted.usrhora, inserted.usrinis,inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,inserted.fdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.cmccautoreg,td.cmccautoregn,'FT',inserted.cobranca, inserted.cambiofixo,case when td.tipodoc=3 then 0 else inserted.total-inserted.rdtotal end,case when td.tipodoc=3 then 0 else inserted.etotal-inserted.erdtotal end,case when td.tipodoc=3 then 0 else inserted.totalmoeda-inserted.rdtotalm end,case when td.tipodoc=3 then 0 else inserted.total-inserted.rdtotal end,case when td.tipodoc=3 then 0 else inserted.etotal-inserted.erdtotal end,case when td.tipodoc=3 then 0 else inserted.totalmoeda-inserted.rdtotalm end,case when td.tipodoc<>3 then 0 else (inserted.total-inserted.rdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.etotal-inserted.erdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.totalmoeda-inserted.rdtotalm)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.total-inserted.rdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.etotal-inserted.erdtotal)*-1 end,case when td.tipodoc<>3 then 0 else (inserted.totalmoeda-inserted.rdtotalm)*-1 end,inserted.ivav1, inserted.ivav2, inserted.ivav3, inserted.ivav4, inserted.ivav5, inserted.ivav6, inserted.ivav7, inserted.ivav8, inserted.ivav9,inserted.eivav1, inserted.eivav2, inserted.eivav3, inserted.eivav4, inserted.eivav5, inserted.eivav6, inserted.eivav7, inserted.eivav8, inserted.eivav9,inserted.ivatx1, inserted.ivatx2, inserted.ivatx3, inserted.ivatx4, inserted.ivatx5, inserted.ivatx6, inserted.ivatx7, inserted.ivatx8, inserted.ivatx9,ft2.reexgiva,dateadd(Month,13, inserted.fdata-DAY(inserted.fdata)),ft2.formapag,'1', inserted.clbanco, inserted.clcheque,inserted.moeda, inserted.fno, inserted.pdata, inserted.fref, inserted.intid, inserted.cobrador, inserted.rota, inserted.ccusto, inserted.ncusto, inserted.zona, inserted.segmento, inserted.tipo,case when td.lancacli=0 then inserted.pais else ft2.c2pais end,case when td.lancacli=0 then inserted.estab else ft2.c2estab end, inserted.vendedor, inserted.vendnm, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.chtotal else 0 end, case when inserted.cheque=1 and td.tipodoc<>3 then inserted.echtotal else 0 end  from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp  where td.lancacc=1 and td.ccautoreg=1 and inserted.multi=0 IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancaol=1)>0INSERT INTO OL (olstamp,ftstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,entrm,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext, ollocal,contado,local,moeda,  acerto, eacerto, macerto, vdinheiro, evdinheiro, mvdinheiro, chtotal, echtotal, chtmoeda, modop1, epaga1, paga1, mpaga1, modop2, epaga2, paga2, mpaga2, modop3, epaga3, paga3, mpaga3, modop4, epaga4, paga4, mpaga4, modop5, epaga5, paga5, mpaga5, modop6, epaga6, paga6, mpaga6 ) select inserted.ftstamp, inserted.ftstamp,inserted.usrdata, inserted.usrhora, inserted.usrinis,inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,'FT',inserted.nmdoc,convert(char(15), inserted.fno),inserted.fdata, inserted.fdata, inserted.total-inserted.rdtotal-inserted.virs - isnull(ft3.ivacativado, 0) , inserted.etotal-inserted.erdtotal-inserted.evirs - isnull(ft3.eivacativado, 0) , inserted.valorm2,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.fref, inserted.intid, inserted.ccusto, inserted.ncusto,inserted.site, inserted.pnome, inserted.pno, inserted.cxstamp, inserted.cxusername,inserted.ssstamp, inserted.ssusername,isnull(ft2.operext, 0),case when td.altollocal = 1 then ft2.vdollocal else td.vdollocal end,case when td.altollocal = 1 then ft2.vdcontado else td.vdcontado end,case when td.altollocal = 1 then ft2.vdlocal else td.vdlocal end,case when td.altollocal = 1 then ft2.olmoeda else td.olmoeda end, ft2.acerto, ft2.eacerto, ft2.macerto, case when ft2.parcial<>0 then ft2.parcial else case when ft2.eparcial=0 then ft2.vdinheiro else 0 end end, case when ft2.eparcial<>0 then ft2.eparcial else case when ft2.parcial=0 then ft2.evdinheiro else 0 end end, ft2.mvdinheiro,inserted.chtotal, inserted.echtotal, inserted.chtmoeda, ft2.modop1, ft2.epaga1, ft2.paga1, ft2.mpaga1, ft2.modop2, ft2.epaga2, ft2.paga2, ft2.mpaga2, ft2.modop3, ft2.epaga3, ft2.paga3, ft2.mpaga3, ft2.modop4, ft2.epaga4, ft2.paga4, ft2.mpaga4, ft2.modop5, ft2.epaga5, ft2.paga5, ft2.mpaga5, ft2.modop6, ft2.epaga6, ft2.paga6, ft2.mpaga6 from inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp inner join ft3 on ft3.ft3stamp = inserted.ftstamp inner join oc on oc.olcodigo=td.olcodigo where td.lancaol=1 IF (select count(*) from inserted inner join td on td.ndoc=inserted.ndoc where td.lancacc=0 and inserted.rdtotal<>0 and inserted.tipodoc=1)>0INSERT INTO CC (ccstamp,ftstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,cambiofixo,deb,edeb,debm,formapag,situacao,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,segmento,tipo,pais,estab,vendedor,vendnm) select inserted.ftstamp, inserted.ftstamp,inserted.usrdata, inserted.usrhora, inserted.usrinis,inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,inserted.fdata,case when td.lancacli=0 then inserted.no else ft2.c2no end,case when td.lancacli=0 then inserted.nome else ft2.c2nome end,td.cmcc,td.cmccn,'FT',inserted.cobranca, inserted.cambiofixo, inserted.rdtotal, inserted.erdtotal,inserted.rdtotalm,ft2.formapag,'1',inserted.moeda, inserted.fno, inserted.pdata, inserted.fref, inserted.intid, inserted.cobrador, inserted.rota, inserted.ccusto, inserted.ncusto, inserted.zona, inserted.segmento, inserted.tipo,case when td.lancacli=0 then inserted.pais else ft2.c2pais end,case when td.lancacli=0 then inserted.estab else ft2.c2estab end, inserted.vendedor, inserted.vendnmfrom inserted inner join td on td.ndoc=inserted.ndoc inner join ft2 on ft2.ft2stamp = inserted.ftstamp where td.lancacc=0 and inserted.multi=0 and inserted.rdtotal<>0 and inserted.tipodoc=1if (select count(*) from inserted where inserted.lrstamp<>'')>0 UPDATE LR SET lr.edebi=lr.edebi+inserted.total,lr.eedebi=lr.eedebi+inserted.etotal from inserted where lr.lrstamp=inserted.lrstamp if (select count(*) from inserted where inserted.lpstamp<>'')>0 UPDATE LP SET lp.edebi=lp.edebi+inserted.total,lp.eedebi=lp.eedebi+inserted.etotal from inserted where lp.lpstamp=inserted.lpstamp IF (select count(*) from inserted where inserted.anulado = 1) > 0 UPDATE ft set ft.anulado = inserted.anulado from ft inner join inserted on ft.ftstamp = inserted.ftstamp where inserted.anulado = 1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fc_insert ON FC WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONdeclare @m_banchab char(60), @m_contado float
select @m_banchab = ISNULL((select valor from para1 where descricao='m_banchab'),'')
SELECT @m_contado = ISNULL((SELECT TOP 1 noconta FROM bl WHERE bl.banco = SUBSTRING(@m_banchab, 1, 10) AND bl.conta = SUBSTRING(@m_banchab, 12, 20)),1)
UPDATE FL SET
fl.saldo = case when fl.saldo + isnull((select sum((inserted.cred-inserted.deb+inserted.difcambio)
 + case when (inserted.cred-inserted.deb+inserted.difcambio) > 0 then -inserted.ivacativado else inserted.ivacativado end)
 from inserted where inserted.no=fl.no and inserted.cm <>0),0) <> 0
 then fl.saldo + isnull((select sum((inserted.cred-inserted.deb+inserted.difcambio)
 + case when (inserted.cred-inserted.deb+inserted.difcambio) > 0 then -inserted.ivacativado else inserted.ivacativado end)
 from inserted where inserted.no=fl.no and inserted.cm <>0),0) else 0 end,
 fl.esaldo = case when fl.esaldo + isnull((select sum((inserted.ecred-inserted.edeb+inserted.edifcambio)
 + case when (inserted.ecred-inserted.edeb+inserted.edifcambio) > 0 then -inserted.eivacativado else inserted.eivacativado end)
 from inserted where inserted.no=fl.no and inserted.cm <>0),0) <> 0
 then fl.esaldo + isnull((select sum((inserted.ecred-inserted.edeb+inserted.edifcambio)
 + case when (inserted.ecred-inserted.edeb+inserted.edifcambio) > 0 then -inserted.eivacativado else inserted.eivacativado end)
 from inserted where inserted.no=fl.no and inserted.cm <>0),0) else 0 end
 from INSERTED where fl.no=inserted.no
IF UPDATE(no) or UPDATE(cred) or UPDATE(deb) or UPDATE(ecred) or UPDATE(edeb) or UPDATE(cr) or UPDATE(cradoc) UPDATE fc SET
fc.credf = fc.credf+inserted.deb, fc.debf = fc.debf+inserted.cred,
fc.ecredf = fc.ecredf+inserted.edeb, fc.edebf = fc.edebf+inserted.ecred
from INSERTED where fc.cm=inserted.cr and fc.adoc=inserted.cradoc and fc.no=inserted.no
and inserted.cr > 0 and inserted.cradoc <> SPACE(10)
INSERT INTO OD (fcstamp,odstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,origem,
documento,cheque,
data,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,moeda,boacobranca)
select inserted.fcstamp,inserted.fcstamp,
inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'FC',
inserted.cmdesc,inserted.adoc,
inserted.dataven,(inserted.cred-inserted.credf)-(inserted.deb-inserted.debf) - case when cm1.debito = 1 then -1 else 1 end *(inserted.ivacativado - inserted.ivacatreg),
(inserted.ecred-inserted.ecredf)-(inserted.edeb-inserted.edebf) - case when cm1.debito = 1 then -1 else 1 end * (inserted.eivacativado - inserted.eivacatreg),
'Pag :'+inserted.nome,cm1.olfl,cm1.flgrupo,cm1.flsgrupo,
inserted.fref,inserted.ccusto,inserted.ncusto,@m_banchab,@m_contado, case when @m_contado>0 then 'B' else 'C' end,'PTE ou EURO'
 ,cm1.boacobrancafo
from inserted inner join fl on fl.no=inserted.no and fl.estab=inserted.estab inner join cm1 on cm1.cm=inserted.cm where
((inserted.cred-inserted.credf)-(inserted.deb-inserted.debf)<>0 and (inserted.ecred-inserted.ecredf)-(inserted.edeb-inserted.edebf)<>0)
 and (inserted.origem <> 'PD' or
 			(inserted.origem = 'PD' and not exists (select 1 from pd where pd.cheque <> '' and pd.pdstamp = inserted.pdstamp)))
 and (inserted.origem <> 'FO' or
 			(inserted.origem = 'FO' and not exists (select 1 from fo2 where fo2.cheque <> '' and fo2.fo2stamp = inserted.fostamp)))

if (select COUNT(*) from inserted where inserted.formapag='')>0
UPDATE fc 
SET formapag ='1'
from inserted
WHERE fc.fcstamp =inserted.fcstamp and fc.formapag=''

if (select COUNT(*) from inserted where inserted.situacao='')>0
UPDATE fc SET 
situacao =case when inserted.cred <>inserted.credf or inserted.deb<>inserted.debf OR inserted.ecred<>inserted.ecredf OR inserted.edeb<>inserted.edebf
then '1'
else '2' end
from inserted
WHERE fc.fcstamp=inserted.fcstamp AND fc.situacao=''GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftcc_update ON ftcc WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONINSERT INTO CH (chstamp,ftstamp,ftccstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,clbanco,clcheque, contado, ollocal) select inserted.ftccstamp,inserted.ftstamp,inserted.ftccstamp,ft.usrdata,ft.usrhora,ft.usrinis,ft.ousrdata,ft.ousrhora,ft.ousrinis,inserted.dataven,ft.ousrdata,ft.ousrdata,case when td.lancacli=0 then ft.no else ft2.c2no end,case when td.lancacli=0 then ft.nome else ft2.c2nome end,case when td.lancacli=0 then ft.estab else ft2.c2estab end,'FTCC',inserted.deb,inserted.edeb,inserted.debm,ft.moeda,inserted.clbanco,inserted.clcheque,case when td.lancaol = 1 then td.vdcontado else 0 end, case when td.lancaol = 1 then td.vdollocal else SPACE(50) end from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=ft.ftstamp inner join td on td.ndoc=ft.ndoc  where ft.anulado=0 and inserted.cheque=1 and inserted.ftccstamp not IN (select deleted.ftccstamp from deleted where deleted.cheque=1) UPDATE CH set ch.usrdata=ft.usrdata,ch.usrhora=ft.usrhora,ch.usrinis=ft.usrinis,ch.data=inserted.dataven,ch.no=case when td.lancacli=0 then ft.no else ft2.c2no end,ch.nome=case when td.lancacli=0 then ft.nome else ft2.c2nome end,ch.estab=case when td.lancacli=0 then ft.estab else ft2.c2estab end,ch.valor=inserted.deb,ch.evalor=inserted.edeb,ch.valorm=inserted.debm,ch.moeda=ft.moeda,ch.clcheque=inserted.clcheque,ch.clbanco=inserted.clbanco  from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=ft.ftstamp inner join td on ft.ndoc=td.ndoc where ft.anulado=0 and inserted.ftccstamp=ch.ftccstamp and inserted.cheque=1 DELETE FROM CH FROM inserted where inserted.cheque=0 and inserted.ftccstamp in (select ch.chstamp from ch where ch.ftccstamp=inserted.ftccstamp) and inserted.ftccstamp=ch.chstamp UPDATE CC set  cc.valch=cc.valch-case when deleted.cheque=1 then deleted.deb else 0 end, cc.evalch=cc.evalch-case when deleted.cheque=1 then deleted.edeb else 0 end  from deleted inner join ft on ft.ftstamp=deleted.ftstamp inner join td on ft.ndoc=td.ndoc where ft.anulado = 0 and deleted.ftccstamp=cc.ftccstamp UPDATE CC set cc.usrdata=ft.usrdata,cc.usrhora=ft.usrhora,cc.usrinis=ft.usrinis, cc.datalc=ft.fdata,cc.no=case when td.lancacli=0 then ft.no else ft2.c2no end,cc.nome=case when td.lancacli=0 then ft.nome else ft2.c2nome end,cc.cm=td.cmmcc,cc.cmdesc=td.cmmccn,cc.cobranca=ft.cobranca,cc.cambiofixo=ft.cambiofixo,cc.moeda=ft.moeda,cc.nrdoc=ft.fno,cc.dataven=inserted.dataven,cc.fref=ft.fref,cc.ccusto=ft.ccusto,cc.ncusto=ft.ncusto,cc.zona=ft.zona,cc.segmento=ft.segmento,cc.tipo=ft.tipo,cc.pais=case when td.lancacli=0 then ft.pais else ft2.c2pais end,cc.estab=case when td.lancacli=0 then ft.estab else ft2.c2estab end,cc.vendedor=ft.vendedor,cc.vendnm=ft.vendnm,cc.ivav1=inserted.ivav1,cc.ivav2=inserted.ivav2,cc.ivav3=inserted.ivav3,cc.ivav4=inserted.ivav4,cc.ivav5=inserted.ivav5,cc.ivav6=inserted.ivav6,cc.ivav7=inserted.ivav7,cc.ivav8=inserted.ivav8,cc.ivav9=inserted.ivav9,cc.eivav1=inserted.eivav1,cc.eivav2=inserted.eivav2,cc.eivav3=inserted.eivav3,cc.eivav4=inserted.eivav4,cc.eivav5=inserted.eivav5,cc.eivav6=inserted.eivav6,cc.eivav7=inserted.eivav7,cc.eivav8=inserted.eivav8,cc.eivav9=inserted.eivav9,cc.IVATX1=ft.IVATX1,cc.IVATX2=ft.IVATX2,cc.IVATX3=ft.IVATX3,cc.IVATX4=ft.IVATX4,cc.IVATX5=ft.IVATX5,cc.IVATX6=ft.IVATX6,cc.IVATX7=ft.IVATX7,cc.IVATX8=ft.IVATX8,cc.IVATX9=ft.IVATX9,cc.REEXGIVA=td.REEXGIVA,cc.intid=ft.intid,cc.formapag=inserted.formapag,cc.deb=case when td.tipodoc<>3 then inserted.debcc else 0 end,cc.edeb=case when td.tipodoc<>3 then inserted.edebcc else 0 end,cc.debm=case when td.tipodoc<>3 then inserted.debmcc else 0 end,cc.debf=case when td.tipodoc<>3 then cc.debf-(deleted.debcc-deleted.deb)+(inserted.debcc-inserted.deb) else 	(cc.debf-(deleted.debcc-deleted.deb)+(inserted.debcc-inserted.deb))*-1 end, cc.edebf=case when td.tipodoc<>3 then cc.edebf-(deleted.edebcc-deleted.edeb)+(inserted.edebcc-inserted.edeb) else  	(cc.edebf-(deleted.edebcc-deleted.edeb)+(inserted.edebcc-inserted.edeb))*-1 END,cc.debfm=case when td.tipodoc<>3 then cc.debfm-(deleted.debmcc-deleted.debm)+(inserted.debmcc-inserted.debm) else 	(cc.debfm-(deleted.debmcc-deleted.debm)+(inserted.debmcc-inserted.debm))*-1 END, cc.obs=inserted.obs,cc.clcheque=inserted.clcheque,cc.clbanco=inserted.clbanco,cc.valch=cc.valch+case when inserted.cheque=1 then inserted.deb else 0 end,cc.evalch=cc.evalch+case when inserted.cheque=1 then inserted.edeb else 0 end, cc.evirs = inserted.evirs, cc.virs=inserted.virs , cc.cobrador=ft.cobrador,cc.rota=ft.rota from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=ft.ftstamp inner join td on ft.ndoc=td.ndoc inner join deleted on deleted.ftccstamp=inserted.ftccstamp where ft.anulado = 0 and inserted.ftccstamp=cc.ftccstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftrd_update ON ftrd WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDELETE from CC from deleted where cc.ccstamp='C'+deleted.ftrdstamp or cc.ccstamp='D'+deleted.ftrdstamp UPDATE CC SET cc.difcambio = cc.difcambio - ISNULL((select SUM(deleted.difcambio) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), 0), cc.edifcambio = cc.edifcambio - ISNULL((select SUM(deleted.edifcambio) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), 0), cc.credf=cc.credf - (select SUM(deleted.vreg) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), cc.ecredf=cc.ecredf - (select SUM(deleted.evreg) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp), cc.credfm=cc.credfm - (select SUM(deleted.vmreg) from deleted where deleted.ccstamp <> '' and deleted.ccstamp = cc.ccstamp)  from cc inner join deleted on deleted.ccstamp=cc.ccstamp where deleted.ccstamp <> '' update cc set cc.debf=cc.debf - (select SUM(deleted.vreg) from deleted where deleted.ccstamp <> '' and deleted.ftstamp = cc.ftstamp and deleted.multi = 0), cc.edebf=cc.edebf - (select SUM(deleted.evreg) from deleted where deleted.ccstamp <> '' and deleted.ftstamp = cc.ftstamp and deleted.multi = 0), cc.debfm=cc.debfm - (select SUM(deleted.vmreg) from deleted where deleted.ccstamp <> '' and deleted.ftstamp = cc.ftstamp and deleted.multi = 0) from cc inner join ft on ft.ftstamp = cc.ftstamp inner join deleted on deleted.ftstamp=cc.ftstamp inner join td on td.ndoc = ft.ndoc where ft.anulado = 0 and cc.ccstamp<>'R'+deleted.ftstamp and ft.multi = 0 and deleted.multi = 0 and deleted.ccstamp <> '' and td.ccautoreg = 0 update rd set rd.integrado=(case isnull((select top 1 ftrd.ftrdstamp from ftrd where ftrd.rdstamp=deleted.rdstamp),'') when '' then 0 else 1 end) from deleted where rd.rdstamp=deleted.rdstamp INSERT INTO CC (ccstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,cred,ecred,credm,credf,ecredf,credfm,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs) select 'C'+inserted.ftrdstamp,cc.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,cc.datalc,case when td.lancacli=0 then ft.no else ft2.c2no end,case when td.lancacli=0 then ft.nome else ft2.c2nome end,cc.cm,cc.cmdesc,'RD',cc.cobranca,inserted.vreg,inserted.evreg,inserted.vmreg,inserted.vreg,inserted.evreg,inserted.vmreg,cc.ivav1,cc.ivav2,cc.ivav3,cc.ivav4,cc.ivav5,cc.ivav6,cc.ivav7,cc.ivav8,cc.ivav9,cc.eivav1,cc.eivav2,cc.eivav3,cc.eivav4,cc.eivav5,cc.eivav6,cc.eivav7,cc.eivav8,cc.eivav9,cc.moeda,cc.nrdoc,cc.dataven,cc.fref,cc.intid,cc.cobrador,cc.rota,cc.ccusto,cc.ncusto,cc.zona,cc.vendedor,cc.vendnm,cc.segmento,cc.tipo,cc.pais,case when td.lancacli=0 then ft.estab else ft2.c2estab end,'Do cliente nº '+ltrim(str(cc.no)) from inserted,ft,ft2,td,cc where ft.anulado = 0 and cc.ccstamp=inserted.ccstamp and ft.ftstamp=inserted.ftstamp and ft2.ft2stamp=ft.ftstamp and td.ndoc=ft.ndoc and case when td.lancacli=0 then ft.no else ft2.c2no end<>cc.no INSERT INTO CC (ccstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,deb,edeb,debm,debf,edebf,debfm,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs) select 'D'+inserted.ftrdstamp,cc.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,cc.datalc,cc.no,cc.nome,cc.cm,cc.cmdesc,'RD',cc.cobranca,inserted.vreg,inserted.evreg,inserted.vmreg,inserted.vreg,inserted.evreg,inserted.vmreg,cc.ivav1,cc.ivav2,cc.ivav3,cc.ivav4,cc.ivav5,cc.ivav6,cc.ivav7,cc.ivav8,cc.ivav9,cc.eivav1,cc.eivav2,cc.eivav3,cc.eivav4,cc.eivav5,cc.eivav6,cc.eivav7,cc.eivav8,cc.eivav9,cc.moeda,cc.nrdoc,cc.dataven,cc.fref,cc.intid,cc.cobrador,cc.rota,cc.ccusto,cc.ncusto,cc.zona,cc.vendedor,cc.vendnm,cc.segmento,cc.tipo,cc.pais,cc.estab,'Regularizado no doc. '+rtrim(ft.nmdoc)+' nº '+ltrim(str(ft.fno)) from inserted,ft,ft2,td,cc where ft.anulado = 0 and cc.ccstamp=inserted.ccstamp and ft.ftstamp=inserted.ftstamp and ft2.ft2stamp=ft.ftstamp and td.ndoc=ft.ndoc and case when td.lancacli=0 then ft.no else ft2.c2no end<>cc.noUPDATE CC SET cc.difcambio = cc.difcambio + (select SUM(inserted.difcambio) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.edifcambio = cc.edifcambio + (select SUM(inserted.edifcambio) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.credf = cc.credf + (select SUM(inserted.vreg) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.ecredf = cc.ecredf + (select SUM(inserted.evreg) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.credfm = cc.credfm + (select SUM(inserted.vmreg) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp) from cc inner join inserted on cc.ccstamp = inserted.ccstamp where inserted.ccstamp <> '' update cc set cc.debf = cc.debf + (select SUM(inserted.vreg) from inserted where inserted.multi = 0 and inserted.ftstamp <> '' and inserted.ftstamp = cc.ftstamp), cc.edebf = cc.edebf + (select SUM(inserted.evreg) from inserted where inserted.multi = 0 and inserted.ftstamp <> '' and inserted.ftstamp = cc.ftstamp), cc.debfm = cc.debfm + (select SUM(inserted.vmreg) from inserted where inserted.multi = 0 and inserted.ftstamp <> '' and inserted.ftstamp = cc.ftstamp) from cc inner join inserted on inserted.ftstamp = cc.ftstamp inner join ft on ft.ftstamp = cc.ftstamp inner join td on td.ndoc = ft.ndoc where ft.anulado = 0 and cc.ccstamp<>'R'+inserted.ftstamp and inserted.multi = 0 and td.ccautoreg = 0 update rd set rd.integrado=1 from inserted where rd.rdstamp=inserted.rdstamp GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lm_insert ON lm WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (Select count(*) from inserted where inserted.cm=11 or inserted.cm=13 or inserted.cm=3 or inserted.cm=7 or inserted.cm=17 or inserted.cm=21)>0 DELETE from OD from inserted where od.lrstamp=inserted.lrstamp and od.origem='LR' and (inserted.cm=11 or inserted.cm=13 or inserted.cm=3 or inserted.cm=7 or inserted.cm=17 or inserted.cm=21)if (Select count(*) from inserted where inserted.cm=1 or inserted.cm=19 or inserted.cm=27 or inserted.cm=20 or inserted.cm=9 or inserted.cm=14 or inserted.cm=26 or inserted.cm=10 or inserted.cm=15 or inserted.cm=5)>0 if (Select count(*) from od,inserted where od.lrstamp=inserted.lrstamp and od.origem='LR')=0 INSERT INTO OD (lrstamp,lmstamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,entr,eentr,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,boacobranca) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LR',lr.nomeserie,ltrim(str(inserted.letra,15,0)),lr.vdata,lr.valor-lr.regvalor,lr.evalor-lr.eregvalor,ltrim(rtrim(lr.nome))+'-'+inserted.lmdesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lr.fref,lr.ccusto,lr.ncusto,inserted.ollocal,inserted.contado,inserted.localol ,case when isnull(cl.cobnao,0) = 0 then 1 else 0 end as boacob from inserted inner join lr on lr.lrstamp=inserted.lrstamp left join cl on cl.nome = lr.nome and cl.no = lr.no and cl.estab = lr.estab inner join oc on oc.olcodigo='R00001' where ((lr.valor-lr.regvalor)<>0 or (lr.evalor-lr.eregvalor)<>0) and (inserted.cm=1 or inserted.cm=19 or inserted.cm=27 or inserted.cm=20 or inserted.cm=9 or inserted.cm=14 or inserted.cm=26 or inserted.cm=10 or inserted.cm=15 or inserted.cm=5) else UPDATE OD SET od.lmstamp=inserted.lmstamp,od.usrdata=inserted.usrdata,od.usrhora=inserted.usrhora,od.usrinis=inserted.usrinis,od.data=lr.vdata,od.entr=(lr.valor-lr.regvalor),od.eentr=(lr.evalor-lr.eregvalor),od.descricao=ltrim(rtrim(lr.nome))+'-'+case when inserted.cm=5 then od.descricao else inserted.lmdesc end,od.fref=lr.fref,od.ccusto=lr.ccusto,od.ncusto=lr.ncusto,od.ollocal=inserted.ollocal,od.contado=inserted.contado,od.local=inserted.localol ,od.boacobranca = case when isnull(cl.cobnao,0) = 0 then 1 else 0 end from inserted inner join lr on lr.lrstamp=inserted.lrstamp left join cl on cl.nome = lr.nome and cl.no = lr.no and cl.estab = lr.estab where od.lrstamp=inserted.lrstamp and od.origem='LR' and (inserted.cm=1 or inserted.cm=19 or inserted.cm=27 or inserted.cm=20 or inserted.cm=9 or inserted.cm=14 or inserted.cm=26 or inserted.cm=10 or inserted.cm=15 or inserted.cm=5) if (Select count(*) from inserted where inserted.cm=21)>0 INSERT INTO CC (lrstamp,ccstamp,lmstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,dataven,no,estab,nome,cm,cmdesc,origem,deb,edeb,nrdoc,fref,intid,ccusto,ncusto,pais,obs,moeda,zona) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,lr.sdata,lr.ccdata,lr.no,lr.estab,lr.nome,30,(select cmdesc from cm1 (nolock) where cm=30),'LR',inserted.valor,inserted.evalor,lr.letra,lr.fref,lr.intid,lr.ccusto,lr.ncusto,lr.pais,'Anulação do título de dívida nº '+ltrim(str(inserted.letra,10,0)),'PTE ou EURO',cl.zona from inserted inner join lr on lr.lrstamp=inserted.lrstamp inner join cl on lr.no=cl.no and lr.estab=cl.estab where inserted.cm=21 and (inserted.valor<>0 or inserted.evalor<>0)if (Select count(*) from inserted where inserted.cm=26)>0 INSERT INTO CC (lrstamp,ccstamp,lmstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,dataven,no,estab,nome,cm,cmdesc,origem,deb,edeb,nrdoc,fref,intid,ccusto,ncusto,pais,obs,moeda,zona) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,lr.sdata,lr.ccdata,lr.no,lr.estab,lr.nome,30,(select cmdesc from cm1 (nolock) where cm=30),'LR',inserted.valor,inserted.evalor,lr.letra,lr.fref,lr.intid,lr.ccusto,lr.ncusto,lr.pais,'Anulação do Título de dívida nº '+ltrim(str(inserted.letra,10,0)) + ' por Recâmbio','PTE ou EURO',cl.zona from inserted inner join lr on lr.lrstamp=inserted.lrstamp inner join cl on lr.no=cl.no and lr.estab=cl.estab where inserted.cm=26 and (inserted.valor<>0 or inserted.evalor<>0)if (Select count(*) from inserted where inserted.cm=11 or inserted.cm=26 or inserted.cm=13 or inserted.cm=3 or inserted.cm=5 or inserted.cm=23)>0 INSERT INTO OL (lrstamp,lmstamp,olstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,moeda) select lr.lrstamp,inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LR',lr.nomeserie+' nº '+ltrim(str(inserted.letra,15,0)),inserted.cheque,inserted.badata,inserted.badata,case when inserted.cm=23 then case when (inserted.despesas+inserted.juros + inserted.iselo)>0 then 0 else (inserted.despesas+inserted.juros+inserted.iselo)*-1 end else case when inserted.cm<>26 then case when ((inserted.valor)-(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end)))>0 then ((inserted.valor)-(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end))) else 0 end else 0 end end,case when inserted.cm=23 then case when (inserted.edespesas+inserted.ejuros + inserted.eiselo)>0 then 0 else (inserted.edespesas+inserted.ejuros + inserted.eiselo)*-1 end else case when inserted.cm<>26 then case when ((inserted.evalor)-(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end)))>0 then ((inserted.evalor)-(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end))) else 0 end else 0 end end,case when inserted.cm=23 then case when (inserted.despesas+inserted.juros + inserted.iselo)>0 then (inserted.despesas+inserted.juros + inserted.iselo) else 0 end else case when inserted.cm<>26 then case when ((inserted.valor)-(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end)))<0 then (((inserted.valor)*-1)+(inserted.despesas+inserted.juros + (case when inserted.cm = 11 then inserted.iselo else 0 end))) else 0 end else (inserted.valor)+inserted.despesas+inserted.juros + inserted.iselo end end,case when inserted.cm=23 then case when (inserted.edespesas+inserted.ejuros + inserted.eiselo)>0 then (inserted.edespesas+inserted.ejuros + inserted.eiselo) else 0 end else case when inserted.cm<>26 then case when ((inserted.evalor)-(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end)))<0 then (((inserted.evalor)*-1)+(inserted.edespesas+inserted.ejuros + (case when inserted.cm = 11 then inserted.eiselo else 0 end))) else 0 end else (inserted.evalor)+inserted.edespesas+inserted.ejuros + inserted.eiselo end end,ltrim(rtrim(lr.nome))+'-'+inserted.lmdesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lr.fref,lr.intid,lr.ccusto,lr.ncusto,inserted.ollocal,inserted.contado,inserted.localol, 'PTE ou EURO' from inserted inner join lr on lr.lrstamp=inserted.lrstamp inner join oc on oc.olcodigo='R00001' where inserted.cm=11 or inserted.cm=26 or inserted.cm=13 or inserted.cm=3 or inserted.cm=5 or inserted.cm=23 if (Select count(*) from inserted where inserted.cm=5 and inserted.lancach = 1)>0 INSERT INTO CH (chstamp,lmstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,data,dataem,datareg,no,nome,estab,origem,valor,evalor,moeda,clbanco,clcheque,contado,ollocal) SELECT inserted.lmstamp,inserted.lmstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.datach,inserted.ousrdata,inserted.ousrdata,lr.no,lr.nome,lr.estab,'LM',inserted.valor,inserted.evalor,'PTE ou EURO',inserted.bancoch,inserted.chequech,inserted.contado,inserted.ollocal FROM inserted INNER JOIN LR ON lr.lrstamp = inserted.lrstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_sl_update ON SL WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update (qtt) or update (ref) or update (vu) or update (cm) or update (lote) or update (cor) or update (tam) or update (datalc) or update (armazem) BEGIN DECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTDECLARE @@STICLASTDATE datetimeSELECT @@STICLASTDATE = (select case when exists(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc)  then convert(datetime,(select top 1 stic.data from stic where stic.lanca = 1 and stic.jusante = 1 order by stic.data desc),121) else convert(datetime,'19000101',121) end) DECLARE @Cinfo VARBINARY(128) SELECT @Cinfo = Context_Info() IF @Cinfo = 0x88888 BEGIN RETURN END IF @@MROW = 1 BEGIN IF exists(select * from inserted inner join deleted on deleted.slstamp = inserted.slstamp where deleted.datalc < @@STICLASTDATE and inserted.datalc < @@STICLASTDATE)RETURN END IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(vu) OR UPDATE(evu) OR UPDATE(ref) OR UPDATE(trfa) OR UPDATE(datalc) IF (select count(*) from deleted where deleted.cm<50)>0 BEGINUPDATE ST SET st.pcpond=case when st.stock-deleted.qtt>0  and deleted.cm<50 and cm2.mudapcpond=1 then (st.pcpond*(case when st.stock>0 then st.stock else 0 end)-deleted.vu*deleted.qtt)/((case when st.stock>0 then st.stock else 0 end)-deleted.qtt) else st.pcpond end,st.epcpond=case when st.stock-deleted.qtt>0  and deleted.cm<50 and cm2.mudapcpond=1 then (st.epcpond*(case when st.stock>0 then st.stock else 0 end)-deleted.evu*deleted.qtt)/((case when st.stock>0 then st.stock else 0 end)-deleted.qtt) else st.epcpond end from deleted inner join cm2 on deleted.cm=cm2.cm where st.ref=deleted.ref and deleted.trfa=0 and deleted.datalc >= @@STICLASTDATE ENDIF @@MROW = 1BEGIN IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(trfa) OR UPDATE(datalc) UPDATE ST SET st.stock=st.stock-case when deleted.cm<50 then deleted.qtt else -deleted.qtt end,st.qttacin=case when deleted.cm<50 then -deleted.QTT+st.qttacin else st.qttacin end, st.qttacout=case when deleted.cm>50 then -deleted.QTT+st.qttacout else st.qttacout end from deleted where st.ref=deleted.ref and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(armazem) OR UPDATE(datalc) UPDATE SA SET sa.stock=sa.stock+case when deleted.cm<50 then -deleted.qtt else deleted.qtt end, sa.qttacin=case when deleted.cm<50 then -deleted.QTT+sa.qttacin else sa.qttacin end from deleted where sa.ref=deleted.ref and sa.armazem=deleted.armazem  and deleted.datalc >= @@STICLASTDATE IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(armazem) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(datalc) UPDATE SX SET sx.stock=sx.stock+case when deleted.cm<50 then -deleted.qtt else deleted.qtt end from deleted where sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam  and deleted.datalc >= @@STICLASTDATE END ELSE BEGIN IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(trfa) OR UPDATE(datalc) UPDATE ST SET st.stock = st.stock -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) end, st.qttacin = st.qttacin -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) end, st.qttacout = st.qttacout -case when (SELECT SUM(case when deleted.cm<50 then 0 else deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then 0 else deleted.qtt end) FROM deleted WHERE deleted.ref=st.ref  and deleted.trfa=0  and deleted.datalc >= @@STICLASTDATE ) end  from deleted WHERE st.ref IN (SELECT ref FROM deleted) and deleted.datalc >= @@STICLASTDATE IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(armazem) OR UPDATE(datalc) UPDATE SA SET sa.stock = sa.stock -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) end, sa.qttacin = sa.qttacin -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else 0 end) FROM deleted WHERE deleted.ref=sa.ref and deleted.armazem=sa.armazem  and deleted.datalc >= @@STICLASTDATE ) end from deleted inner join sa on deleted.ref=sa.ref and deleted.armazem=sa.armazem where deleted.datalc >= @@STICLASTDATE IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(armazem) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(datalc) UPDATE SX SET sx.stock = sx.stock -case when (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam  and deleted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when deleted.cm<50 then deleted.qtt else -deleted.qtt end) FROM deleted WHERE deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam  and deleted.datalc >= @@STICLASTDATE ) end from deleted inner join sx on deleted.ref=sx.ref and deleted.cor=sx.cor and deleted.tam=sx.tam and deleted.armazem=sx.armazem  where deleted.datalc >= @@STICLASTDATE END IF not EXISTS(select sa.ref from sa,inserted where sa.ref=inserted.ref and sa.armazem=inserted.armazem)INSERT INTO SA (sastamp,armazem,ref) select distinct convert(char(5),armazem)+ref,armazem,ref from inserted where not EXISTS(select ref from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and inserted.datalc >= @@STICLASTDATE IF not EXISTS(select sx.ref from sx,inserted where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam)INSERT INTO Sx (sxstamp,armazem,ref,cor,tam,ststamp) select distinct substring(cast(newid() as char(36)),1,25),inserted.armazem,inserted.ref,inserted.cor,inserted.tam,st.ststamp from inserted inner join st on inserted.ref=st.ref where inserted.cor!='' and inserted.tam!='' and not EXISTS(select armazem from sx where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam)  and inserted.datalc >= @@STICLASTDATE GROUP BY inserted.armazem,inserted.ref,inserted.cor,inserted.tam,st.ststamp IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(vu) OR UPDATE(evu) OR UPDATE(ref) OR UPDATE(trfa) OR UPDATE(datalc) IF (select count(*) from inserted where inserted.cm<50)>0 BEGINUPDATE ST SET st.pcpond=case when inserted.qtt+st.stock>0  and inserted.cm<50 and cm2.mudapcpond=1 then (st.pcpond*(case when st.stock>0 then st.stock else 0 end)+inserted.vu*inserted.qtt)/((case when st.stock>0 then st.stock else 0 end)+inserted.qtt) else st.pcpond end,st.epcpond=case when inserted.qtt+st.stock>0  and inserted.cm<50 and cm2.mudapcpond=1 then (st.epcpond*(case when st.stock>0 then st.stock else 0 end)+inserted.evu*inserted.qtt)/((case when st.stock>0 then st.stock else 0 end)+inserted.qtt) else st.epcpond end from INSERTED inner join cm2 on inserted.cm=cm2.cm where st.ref=inserted.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ENDIF @@MROW = 1BEGIN IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(datalc) OR UPDATE(vu) OR UPDATE(evu) OR UPDATE(origem) OR UPDATE(ref) OR UPDATE(trfa) OR UPDATE(datalc) UPDATE ST SET st.stock=st.stock+case when inserted.cm<50 then inserted.qtt else -inserted.qtt end ,  st.uintr=case when inserted.datalc>=st.uintr  and inserted.cm<50 then inserted.datalc else st.uintr end,  st.pcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.vu else st.pcult end,  st.usaid=case when inserted.datalc>=st.usaid  and inserted.cm>50 then inserted.datalc else st.usaid end,  st.pvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50 and inserted.qtt>0 and inserted.origem='FT' then inserted.vu else st.pvultimo end,  st.epcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.evu else st.epcult end,  st.epvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50  and inserted.qtt>0 and inserted.origem='FT'  and inserted.origem='FT' then inserted.evu else st.epvultimo end,  st.qttacout=case when inserted.cm>50 then inserted.QTT+st.qttacout else st.qttacout end,  st.qttacin=case when inserted.cm<50 then inserted.QTT+st.qttacin else st.qttacin end,  st.udata=inserted.datalc  from INSERTED inner join cm2 on inserted.cm=cm2.cm where st.ref=inserted.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE UPDATE SA SET sa.stock=sa.stock+case when inserted.cm<50 then inserted.qtt else -inserted.qtt end, sa.qttacin=case when inserted.cm<50 then inserted.QTT+sa.qttacin else sa.qttacin end from INSERTED where sa.ref=inserted.ref and sa.armazem=inserted.armazem and EXISTS(select * from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and inserted.datalc >= @@STICLASTDATE UPDATE SX SET sx.stock=sx.stock+case when inserted.cm<50 then inserted.qtt else -inserted.qtt end from INSERTED where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and EXISTS(select * from sx where sx.ref=inserted.ref and inserted.armazem=sx.armazem and sx.cor=inserted.cor and sx.tam =inserted.tam) and inserted.datalc >= @@STICLASTDATE END ELSE BEGIN IF UPDATE(CM) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(trfa) OR UPDATE(datalc) UPDATE ST SET st.stock = st.stock +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ) end, st.qttacout = st.qttacout +case when (SELECT SUM(case when inserted.cm<50 then 0 else inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then 0 else inserted.qtt end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ) end, st.qttacin = st.qttacin +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=st.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE ) end  from inserted WHERE st.ref IN (SELECT ref FROM inserted) and inserted.datalc >= @@STICLASTDATE IF  UPDATE(datalc) OR UPDATE(cm) OR UPDATE(vu) OR UPDATE(qtt) OR UPDATE(evu) OR UPDATE(origem) OR UPDATE(trfa) OR UPDATE(datalc) UPDATE ST SET  st.uintr=case when inserted.datalc>=st.uintr  and inserted.cm<50 then inserted.datalc else st.uintr end,  st.pcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.vu else st.pcult end,  st.usaid=case when inserted.datalc>=st.usaid  and inserted.cm>50 then inserted.datalc else st.usaid end,  st.pvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50   and inserted.qtt>0 and inserted.origem='FT' then inserted.vu else st.pvultimo end,  st.epcult=case when inserted.datalc>=st.uintr  and inserted.cm<50 and cm2.mudapcult=1 then inserted.evu else st.epcult end,  st.epvultimo=case when inserted.datalc>=st.usaid  and inserted.cm>50  and inserted.qtt>0  and inserted.origem='FT' then inserted.evu else st.epvultimo end,  st.udata=inserted.datalc  from INSERTED inner join cm2 on inserted.cm=cm2.cm where st.ref=inserted.ref and inserted.trfa=0  and inserted.datalc >= @@STICLASTDATE IF  UPDATE(cm) OR UPDATE(qtt) OR UPDATE(ref) OR UPDATE(armazem) OR UPDATE(datalc) UPDATE SA SET sa.stock = sa.stock +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem  and inserted.datalc >= @@STICLASTDATE ) end, sa.qttacin = sa.qttacin +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem   and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else 0 end) FROM inserted WHERE inserted.ref=sa.ref and inserted.armazem=sa.armazem  and inserted.datalc >= @@STICLASTDATE ) end from inserted inner join sa on inserted.ref=sa.ref and inserted.armazem=sa.armazem where inserted.datalc >= @@STICLASTDATE IF UPDATE(cm) OR UPDATE(qtt) OR UPDATE(armazem) OR UPDATE(ref) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(datalc) UPDATE SX SET sx.stock = sx.stock +case when (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tam  and inserted.datalc >= @@STICLASTDATE ) is null then 0 else (SELECT SUM(case when inserted.cm<50 then inserted.qtt else -inserted.qtt end) FROM inserted WHERE inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tam  and inserted.datalc >= @@STICLASTDATE ) end from inserted inner join sx on inserted.ref=sx.ref and inserted.cor=sx.cor and inserted.tam=sx.tam and inserted.armazem=sx.armazem  where inserted.datalc >= @@STICLASTDATE END  END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_PD_update ON PD WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON if update(pdstamp) or update(usrdata) or update(usrhora) or update(usrinis) or update(rdata)
 or update(no) or update(nome) or update(cm) or update(cmdesc) or update(total) or update(etotal)
 or update(formapag) or update(totalmoeda) or update(virs) or update(evirs)
 or update(tabiva) or update(ivav) or update(eivav) or update(moeda) or update(adoc) or update(fref) or update(intid)
 or update(ccusto) or update(ncusto) or update(zona)or update(tipo) or update(pais)
 or update(estab) or update(descricao) or update(cambiofixo) or update(crend)
 or update(valorm2) or update(moeda2) or update(olcodigo) or update(telocal) or update(contado)
 or update(ollocal) or update(operext) or update(cecope)
 BEGIN
UPDATE FC SET
fc.fcstamp=inserted.pdstamp,fc.pdstamp=inserted.pdstamp,
fc.usrdata=inserted.usrdata,fc.usrhora=inserted.usrhora,fc.usrinis=inserted.usrinis,
fc.datalc=inserted.rdata,fc.no=inserted.no,fc.nome=inserted.nome,fc.cm=inserted.cm,
fc.cmdesc=inserted.cmdesc,fc.origem='PD',
fc.deb=inserted.total,fc.edeb=inserted.etotal,fc.debm=inserted.totalmoeda,
fc.formapag=inserted.formapag,
fc.virs = inserted.virs, fc.evirs = inserted.evirs,
fc.ivav1=case when inserted.tabiva=1 then inserted.ivav else 0 end,
fc.ivav2=case when inserted.tabiva=2 then inserted.ivav else 0 end,
fc.ivav3=case when inserted.tabiva=3 then inserted.ivav else 0 end,
fc.ivav4=case when inserted.tabiva=4 then inserted.ivav else 0 end,
fc.ivav5=case when inserted.tabiva=5 then inserted.ivav else 0 end,
fc.ivav6=case when inserted.tabiva=6 then inserted.ivav else 0 end,
fc.ivav7=case when inserted.tabiva=7 then inserted.ivav else 0 end,
fc.ivav8=case when inserted.tabiva=8 then inserted.ivav else 0 end,
fc.ivav9=case when inserted.tabiva=9 then inserted.ivav else 0 end,
fc.eivav1=case when inserted.tabiva=1 then inserted.eivav else 0 end,
fc.eivav2=case when inserted.tabiva=2 then inserted.eivav else 0 end,
fc.eivav3=case when inserted.tabiva=3 then inserted.eivav else 0 end,
fc.eivav4=case when inserted.tabiva=4 then inserted.eivav else 0 end,
fc.eivav5=case when inserted.tabiva=5 then inserted.eivav else 0 end,
fc.eivav6=case when inserted.tabiva=6 then inserted.eivav else 0 end,
fc.eivav7=case when inserted.tabiva=7 then inserted.eivav else 0 end,
fc.eivav8=case when inserted.tabiva=8 then inserted.eivav else 0 end,
fc.eivav9=case when inserted.tabiva=9 then inserted.eivav else 0 end,
fc.moeda=inserted.moeda,fc.adoc=inserted.adoc,fc.dataven=inserted.rdata,
fc.fref=inserted.fref,fc.intid=inserted.intid,fc.ccusto=inserted.ccusto,fc.ncusto=inserted.ncusto,fc.zona=inserted.zona,
fc.tipo=inserted.tipo,fc.pais=inserted.pais,fc.estab=inserted.estab,fc.obs=inserted.descricao, fc.cambiofixo = inserted.cambiofixo,
fc.crend=inserted.crend,
fc.eivacativado = inserted.eivacativado, fc.ivacativado = inserted.ivacativado,
fc.mivacativado = inserted.mivacativado
from inserted where inserted.pdstamp=fc.pdstamp
and inserted.pdstamp = fc.fcstamp
If (select COUNT(*) from inserted inner join fc on fc.pdstamp = inserted.pdstamp where fc.origem = 'PD' and fc.fcstamp = 'I' + inserted.pdstamp) > 0
UPDATE FC SET
fc.usrdata=inserted.usrdata,fc.usrhora=inserted.usrhora,fc.usrinis=inserted.usrinis,
fc.formapag=inserted.formapag, fc.no=inserted.no, fc.nome=inserted.nome,
fc.deb=case when inserted.virs<0 then 0 else inserted.virs end,fc.edeb=case when inserted.evirs<0 then 0 else inserted.evirs end,
fc.debf=case when inserted.virs<0 then 0 else inserted.virs end,fc.edebf=case when inserted.evirs<0 then 0 else inserted.evirs end,
fc.cred=case when inserted.virs>0 then 0 else -inserted.virs end,fc.ecred=case when inserted.evirs>0 then 0 else -inserted.evirs end,
fc.credf=case when inserted.virs>0 then 0 else -inserted.virs end,fc.ecredf=case when inserted.evirs>0 then 0 else -inserted.evirs end,
fc.fref=inserted.fref,fc.intid=inserted.intid,fc.ccusto=inserted.ccusto,fc.ncusto=inserted.ncusto,fc.zona=inserted.zona,
fc.tipo=inserted.tipo,fc.pais=inserted.pais,fc.estab=inserted.estab,fc.obs=inserted.descricao, fc.cambiofixo = inserted.cambiofixo,
fc.eivacativado = inserted.eivacativado, fc.ivacativado = inserted.ivacativado,
fc.mivacativado = inserted.mivacativado
from inserted where inserted.pdstamp=fc.pdstamp
and 'I' + inserted.pdstamp = fc.fcstamp
If (select COUNT(*) from inserted inner join fc on fc.pdstamp = inserted.pdstamp where fc.origem = 'PD' and fc.fcstamp = 'I' + inserted.pdstamp and (inserted.virs<>0 or inserted.evirs<>0)) = 0
INSERT INTO FC (fcstamp,pdstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
formapag,situacao,
datalc,no,nome,cm,cmdesc,origem,
deb,edeb,debf,edebf,cred,ecred,credf,ecredf,
eivacativado, ivacativado, mivacativado,
moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs)
select 'I'+inserted.pdstamp,inserted.pdstamp,
inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,
inserted.formapag,'1',
inserted.rdata,inserted.no,inserted.nome,
0,
'Indefinido!',
'PD',
case when inserted.virs<0 then 0 else inserted.virs end,case when inserted.evirs<0 then 0 else inserted.evirs end,
case when inserted.virs<0 then 0 else inserted.virs end,case when inserted.evirs<0 then 0 else inserted.evirs end,
case when inserted.virs>0 then 0 else -inserted.virs end,case when inserted.evirs>0 then 0 else -inserted.evirs end,
case when inserted.virs>0 then 0 else -inserted.virs end,case when inserted.evirs>0 then 0 else -inserted.evirs end,
 inserted.eivacativado, inserted.ivacativado, inserted.mivacativado,
'PTE ou EURO',inserted.adoc,inserted.rdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab,inserted.pais,inserted.zona,
inserted.descricao
from inserted where inserted.virs<>0 or inserted.evirs<>0
If (select COUNT(*) from inserted inner join deleted on deleted.pdstamp = inserted.pdstamp inner join fc on fc.pdstamp = inserted.pdstamp
where fc.origem = 'PD' and fc.fcstamp = 'I' + inserted.pdstamp and inserted.virs = 0 and inserted.evirs = 0 and (deleted.virs<>0 or deleted.evirs<>0)) > 0
DELETE FROM FC from inserted where FC.pdstamp = inserted.pdstamp and fc.fcstamp = 'I' + inserted.pdstamp and inserted.virs =  0 and inserted.evirs = 0
UPDATE OL SET
ol.olstamp=inserted.pdstamp,ol.pdstamp=inserted.pdstamp,
ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,
ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='PD',
ol.documento=inserted.cmdesc,ol.cheque=inserted.adoc,
ol.data=inserted.rdata,ol.dvalor=inserted.rdata,ol.said=inserted.total,
ol.esaid=inserted.etotal,
ol.saidm=inserted.valorm2,
ol.moeda=inserted.moeda2,
ol.descricao=inserted.nome,ol.olcodigo=inserted.olcodigo,ol.grupo=oc.olgrupo,ol.sgrupo=oc.olsgrupo,
ol.fref=inserted.fref,ol.intid=inserted.intid,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.telocal,
ol.operext=inserted.operext,ol.cecope=inserted.cecope
from inserted inner join oc on oc.olcodigo=inserted.olcodigo where ol.pdstamp=inserted.pdstamp 

IF update(cheque)
BEGIN 

UPDATE chf
SET deststamp = '' 
, destino = ''
from deleted 
inner join chf on deleted.chfstamp = chf.chfstamp

UPDATE chf
SET deststamp = inserted.pdstamp 
, destino =  'PD' 
, nome = case when chf.nome = '' then inserted.nome else chf.nome end 
, no = case when chf.no = 0 then inserted.no else chf.no end 
, estab = case when chf.no = 0 then inserted.estab else chf.estab end 					
from inserted  
inner join chf on inserted.chfstamp = chf.chfstamp

END 
 END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fc_update ON FC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128) SELECT @Cinfo = Context_Info() IF @Cinfo = 0x22222 RETURN declare @m_banchab char(60), @m_contado floatselect @m_banchab = ISNULL((select valor from para1 where descricao='m_banchab'),'')SELECT @m_contado = ISNULL((SELECT TOP 1 noconta FROM bl WHERE bl.banco = SUBSTRING(@m_banchab, 1, 10) AND bl.conta = SUBSTRING(@m_banchab, 12, 20)),1)IF UPDATE (cred) or UPDATE(deb) or UPDATE(ecred) or UPDATE(edeb) or UPDATE(no) UPDATE FL SET  fl.saldo = case when fl.saldo - isnull((select sum((deleted.cred-deleted.deb+deleted.difcambio)  + case when (deleted.cred-deleted.deb+deleted.difcambio) >0 then -deleted.ivacativado else deleted.ivacativado end)  from DELETED where fl.no=deleted.no and deleted.cm <>0), 0) <> 0  then fl.saldo - isnull((select sum((deleted.cred-deleted.deb+deleted.difcambio)  + case when (deleted.cred-deleted.deb+deleted.difcambio) >0 then -deleted.ivacativado else deleted.ivacativado end)  from DELETED where fl.no=deleted.no and deleted.cm <>0), 0)  else 0 end,  fl.esaldo = case when fl.esaldo - isnull((select sum((deleted.ecred-deleted.edeb+deleted.edifcambio)  + case when (deleted.ecred-deleted.edeb+deleted.edifcambio) > 0 then -deleted.eivacativado else deleted.eivacativado end)  from DELETED where fl.no=deleted.no and deleted.cm <>0), 0) <> 0  then fl.esaldo - isnull((select sum((deleted.ecred-deleted.edeb+deleted.edifcambio)  + case when (deleted.ecred-deleted.edeb+deleted.edifcambio) > 0 then -deleted.eivacativado else deleted.eivacativado end)  from DELETED where fl.no=deleted.no and deleted.cm <>0), 0)  else 0 end  from DELETED where fl.no=deleted.noIF UPDATE (cred) or UPDATE(deb) or UPDATE(ecred) or UPDATE(edeb) or UPDATE(no) UPDATE FL SET  fl.saldo = case when fl.saldo + isnull((select sum((inserted.cred-inserted.deb+inserted.difcambio)  + case when (inserted.cred-inserted.deb+inserted.difcambio) > 0 then -inserted.ivacativado else inserted.ivacativado end)  from inserted where fl.no=inserted.no and inserted.cm <>0), 0) <> 0  then fl.saldo + isnull((select sum((inserted.cred-inserted.deb+inserted.difcambio)  + case when (inserted.cred-inserted.deb+inserted.difcambio) > 0 then -inserted.ivacativado else inserted.ivacativado end)  from inserted where fl.no=inserted.no and inserted.cm <>0), 0)  else 0 end,  fl.esaldo = case when fl.esaldo + isnull((select sum((inserted.ecred-inserted.edeb+inserted.edifcambio)  + case when (inserted.ecred-inserted.edeb+inserted.edifcambio) > 0 then - inserted.eivacativado else inserted.eivacativado end)  from inserted where fl.no=inserted.no and inserted.cm <>0), 0) <> 0  then fl.esaldo + isnull((select sum((inserted.ecred-inserted.edeb+inserted.edifcambio)  + case when (inserted.ecred-inserted.edeb+inserted.edifcambio) > 0 then - inserted.eivacativado else inserted.eivacativado end)  from inserted where fl.no=inserted.no and inserted.cm <>0), 0)  else 0 end  from INSERTED where fl.no=inserted.noIF UPDATE(no) or UPDATE(cred) or UPDATE(deb) or UPDATE(ecred) or UPDATE(edeb) or UPDATE(cr) or UPDATE(cradoc) UPDATE FC SET fc.credf = fc.credf-deleted.deb, fc.debf = fc.debf-deleted.cred, fc.ecredf = fc.ecredf-deleted.edeb, fc.edebf = fc.edebf-deleted.ecred from deleted where fc.cm=deleted.cr and fc.adoc=deleted.cradoc and fc.no=deleted.no and deleted.cr > 0 and deleted.cradoc <> SPACE(10)IF UPDATE(no) or UPDATE(cred) or UPDATE(deb) or UPDATE(ecred) or UPDATE(edeb) or UPDATE(cr) or UPDATE(cradoc) UPDATE FC SET fc.credf = fc.credf+inserted.deb, fc.debf = fc.debf+inserted.cred, fc.ecredf = fc.ecredf+inserted.edeb, fc.edebf = fc.edebf+inserted.ecred from INSERTED where fc.cm=inserted.cr and fc.adoc=inserted.cradoc and fc.no=inserted.no and inserted.cr > 0 and inserted.cradoc <> SPACE(10)IF UPDATE(crend) UPDATE PL SET pl.crend=inserted.crend from INSERTED where pl.fcstamp=inserted.fcstampDELETE FROM OD FROM deleted where deleted.fcstamp=od.odstampINSERT INTO OD (fcstamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,moeda, boacobranca) select inserted.fcstamp,inserted.fcstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'FC',inserted.cmdesc,inserted.adoc,inserted.dataven,(inserted.cred-inserted.credf)-(inserted.deb-inserted.debf) - case when cm1.debito = 1 then -1 else 1 end *(inserted.ivacativado - inserted.ivacatreg),(inserted.ecred-inserted.ecredf)-(inserted.edeb-inserted.edebf) - case when cm1.debito = 1 then -1 else 1 end *(inserted.eivacativado - inserted.eivacatreg),'Pag :'+inserted.nome,cm1.olfl,cm1.flgrupo,cm1.flsgrupo,inserted.fref,inserted.ccusto,inserted.ncusto,@m_banchab,@m_contado, case when @m_contado>0 then 'B' else 'C' end,'PTE ou EURO'  ,cm1.boacobrancafo from inserted inner join fl on fl.no=inserted.no and fl.estab=inserted.estab inner join cm1 on cm1.cm=inserted.cm where ((inserted.cred-inserted.credf)-(inserted.deb-inserted.debf)<>0 and (inserted.ecred-inserted.ecredf)-(inserted.edeb-inserted.edebf)<>0) and (inserted.origem <> 'PD' or   			(inserted.origem = 'PD' and not exists (select 1 from pd where pd.cheque <> '' and pd.pdstamp = inserted.pdstamp)))  and (inserted.origem <> 'FO' or   			(inserted.origem = 'FO' and not exists (select 1 from fo2 where fo2.cheque <> '' and fo2.fo2stamp = inserted.fostamp))) GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_do_insert ON DO WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONIF update(dilno) OR update(ano) OR update(mes) OR update(edebana) OR update(edebord) OR update(edebfin) OR update(ecreana) OR update(ecreord) OR update(ecrefin)	
BEGIN 
UPDATE do
SET dilnoid = ISNULL(CASE WHEN anospc.di_mensal = 1 THEN inserted.mes ELSE inserted.ano END,0) 
From do inner join inserted on do.dostamp = inserted.dostamp
inner join anospc on anospc.ano = inserted.ano
UPDATE di 
SET	di.dilnoano = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano order by inserted.dilno desc),0) > di.dilnoano 
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano order by inserted.dilno desc),0) ELSE di.dilnoano 
END
, di.dilnomes00 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 0 order by inserted.dilno desc),0) > di.dilnomes00
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 0 order by inserted.dilno desc),0) ELSE di.dilnomes00
END
, di.dilnomes01 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 1 order by inserted.dilno desc),0) > di.dilnomes01
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 1 order by inserted.dilno desc),0) ELSE di.dilnomes01
END
, di.dilnomes02 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 2 order by inserted.dilno desc),0) > di.dilnomes02
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 2 order by inserted.dilno desc),0) ELSE di.dilnomes02
END
, di.dilnomes03 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 3 order by inserted.dilno desc),0) > di.dilnomes03
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 3 order by inserted.dilno desc),0) ELSE di.dilnomes03
END
, di.dilnomes04 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 4 order by inserted.dilno desc),0) > di.dilnomes04
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 4 order by inserted.dilno desc),0) ELSE di.dilnomes04
END
, di.dilnomes05 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 5 order by inserted.dilno desc),0) > di.dilnomes05
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 5 order by inserted.dilno desc),0) ELSE di.dilnomes05
END
, di.dilnomes06 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 6 order by inserted.dilno desc),0) > di.dilnomes06
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 6 order by inserted.dilno desc),0) ELSE di.dilnomes06
END
, di.dilnomes07 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 7 order by inserted.dilno desc),0) > di.dilnomes07
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 7 order by inserted.dilno desc),0) ELSE di.dilnomes07
END
, di.dilnomes08 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 8 order by inserted.dilno desc),0) > di.dilnomes08
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 8 order by inserted.dilno desc),0) ELSE di.dilnomes08
END
, di.dilnomes09 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 9 order by inserted.dilno desc),0) > di.dilnomes09
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 9 order by inserted.dilno desc),0) ELSE di.dilnomes09
END
, di.dilnomes10 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 10 order by inserted.dilno desc),0) > di.dilnomes10
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 10 order by inserted.dilno desc),0) ELSE di.dilnomes10
END
, di.dilnomes11 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 11 order by inserted.dilno desc),0) > di.dilnomes11
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 11 order by inserted.dilno desc),0) ELSE di.dilnomes11
END
, di.dilnomes12 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 12 order by inserted.dilno desc),0) > di.dilnomes12
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 12 order by inserted.dilno desc),0) ELSE di.dilnomes12
END
, di.dilnomes13 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 13 order by inserted.dilno desc),0) > di.dilnomes13
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 13 order by inserted.dilno desc),0) ELSE di.dilnomes13
END
, di.dilnomes14 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 14 order by inserted.dilno desc),0) > di.dilnomes14
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 14 order by inserted.dilno desc),0) ELSE di.dilnomes14
END
, di.dilnomes15 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 15 order by inserted.dilno desc),0) > di.dilnomes15
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 15 order by inserted.dilno desc),0) ELSE di.dilnomes15
END
, di.dilnomes16 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 16 order by inserted.dilno desc),0) > di.dilnomes16
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 16 order by inserted.dilno desc),0) ELSE di.dilnomes16
END 
, di.edeb = di.edeb + ISNULL((SELECT SUM(inserted.edebana+inserted.edebord+inserted.edebfin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0)
, di.deb = di.deb + ISNULL((SELECT SUM(inserted.debana+inserted.debord+inserted.debfin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0) 			
, di.cre=di.cre + ISNULL((SELECT SUM(inserted.creana+inserted.creord+inserted.crefin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0) 	
, di.ecre=di.ecre + ISNULL((SELECT SUM(inserted.ecreana+inserted.ecreord+inserted.ecrefin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0)  			
FROM inserted INNER JOIN di on inserted.dino=di.dino and di.diano = inserted.ano	 update di set di.dilno=inserted.dilno+1 from inserted where inserted.dino=di.dino and inserted.dilno >= di.dilno  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fi_insert ON fi WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.lancasl=1 and inserted.ref<>space(18))>0 INSERT INTO SL (slstamp,fistamp,stns,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,segmento,codigo,moeda,datalc,design,ref,lote,armazem,qtt,tt,vu,ett,evu,num1,frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,vumoeda,ttmoeda,pcpond,cpoc,valiva,evaliva,epcpond )select inserted.fistamp,inserted.fistamp,inserted.stns,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',case when td.lifref=1 then inserted.fifref else ft.fref end,case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,ft.nome,ft.segmento,inserted.codigo,ft.moeda,ft.fdata,inserted.design,inserted.ref,inserted.lote,inserted.armazem,case when ft.tipodoc=3 then inserted.qtt*-1 else inserted.qtt end,inserted.sltt,inserted.slvu,inserted.esltt,inserted.eslvu,inserted.num1,ft.no,convert(char (10), ft.fno),td.cmsl,td.cmsln,inserted.composto,inserted.cor,inserted.tam,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.slvumoeda,inserted.slttmoeda,inserted.pcp,inserted.cpoc,case when inserted.iectin<>0 then inserted.pv-inserted.vbase else case when inserted.ivaincl=1 then inserted.tiliquido-inserted.tiliquido/(1+inserted.iva/100) else inserted.tiliquido*inserted.iva/100 end end,case when inserted.eiectin<>0 then inserted.epv-inserted.evbase else case when inserted.ivaincl=1 then inserted.etiliquido-inserted.etiliquido/(1+inserted.iva/100) else inserted.etiliquido*inserted.iva/100 end end,inserted.epcp from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.lancasl=1 and inserted.ref<>space(18) if (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo))>0 INSERT INTO SV (svstamp,fistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fistamp,inserted.fistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',ft.nome,'PTE ou EURO',ft.fdata,inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem,inserted.qtt*(inserted.custo-inserted.pcp),inserted.qtt*(inserted.ecusto-inserted.epcp),ft.no,convert(char (10), ft.fno),td.cmsv,td.cmsvn from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on ft.ndoc=td.ndoc where td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo) IF (select count(*) from inserted where inserted.tipodoc=3 and inserted.ftregstamp<>space(25)  )>0INSERT INTO FTREG (ftregstamp)select inserted.ftregstamp from inserted where inserted.ftregstamp<>space(25) and not EXISTS(select ftregstamp from ftreg where ftreg.ftregstamp=inserted.ftregstamp) IF (select count(*) from inserted where  inserted.tipodoc=3 and inserted.ftregstamp<>space(25) ) >0 UPDATE FTREG SET  ftreg.vciva=ftreg.vciva+  (select ROUND(sum(case when inserted.ivaincl=1 then ((inserted.tiliquido)*-1)*(1-ft.fin/100) else -((inserted.tiliquido-round((inserted.tiliquido*ft.fin/100),0))*(1+inserted.iva/100)) end),0)  from inserted, ft where inserted.ftregstamp=ftreg.ftregstamp and inserted.ftstamp = ft.ftstamp),   ftreg.vmoeda=ftreg.vmoeda+  (select ROUND(sum(case when inserted.ivaincl=1 then ((inserted.tmoeda)*-1)*(1-ft.fin/100) else -((inserted.tmoeda-round((inserted.tmoeda*ft.fin/100),2))*(1+inserted.iva/100)) end),2)   from inserted, ft where inserted.ftregstamp=ftreg.ftregstamp and inserted.ftstamp = ft.ftstamp),   ftreg.evciva=ftreg.evciva+  (select ROUND(sum(case when inserted.ivaincl=1 then ((inserted.etiliquido)*-1)*(1-ft.fin/100) else -((inserted.etiliquido-round((inserted.etiliquido*ft.fin/100),2))*(1+inserted.iva/100)) end),2)   from inserted, ft where inserted.ftregstamp=ftreg.ftregstamp and inserted.ftstamp = ft.ftstamp),  ftreg.ultdoc=(select ltrim(rtrim(ft.nmdoc))+' nº '+convert(char(10),ft.fno)+' de '+convert(char(10),ft.fdata,104) from ft where inserted.ftstamp=ft.ftstamp),  ftreg.fin = isnull((select top 1 ft.fin from ft left join inserted on inserted.ftstamp=ft.ftstamp  where inserted.ftregstamp=ftreg.ftregstamp),0) from ftreg inner join inserted on ftreg.ftregstamp=inserted.ftregstamp where inserted.tipodoc=3 and inserted.ftregstamp<>space(25)IF (select count(*) from inserted where inserted.bistamp<>'' )>0 BEGIN UPDATE BI SET bi.fdata = ft.fdata,bi.fno = ft.fno,bi.adoc = convert(char(10),ft.fno),bi.nmdoc = ft.nmdoc,bi.oftstamp = ft.ftstamp,bi.ndoc = ft.ndoc, bi.qtt2 = bi.qtt2+isnull((select SUM(inserted.qtt*(case when inserted.tipodoc=3 then -1 else 1 end)) from inserted inner join fi2 on fi2.fi2stamp=inserted.fistamp where inserted.bistamp <> '' and inserted.bistamp = bi.bistamp and fi2.noupdqtt2ori = 0),0)from bi inner join inserted on inserted.bistamp=bi.bistamp inner join ft (nolock) on inserted.ftstamp=ft.ftstamp where inserted.bistamp<>'' UPDATE BI SET bi.partes2 = bi.partes2+(select SUM(inserted.partes*(case when inserted.tipodoc=3 then -1 else 1 end)) from inserted where inserted.bistamp <> '' and inserted.bistamp = bi.bistamp) from bi inner join inserted on inserted.bistamp = bi.bistamp inner join ts (nolock) on ts.ndos = bi.ndos where inserted.bistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1)END if (select count(*) from inserted where ndocft<>0 and fnoft<>0 ) >0 UPDATE FT SET ft.facturada = 1 , ft.nmdocft = td.nmdoc,  ft.fnoft = inserted.fno  from inserted inner join td on td.ndoc=inserted.ndoc  where inserted.ndocft<>0 and inserted.ndocft = ft.ndoc and inserted.fnoft = ft.fno  and inserted.ftanoft = ft.ftano if (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.coms=1 and inserted.qtt<>0 and inserted.composto=0)>0 INSERT INTO PN (classe,snstamp,pnstamp,fistamp,ftstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,segmento,fdata,design,ref,qtt,tiliquido,pv,etiliquido,epv,valbase,evalbase,valuni,evaluni,num1,texteis,cor,tam,usalote,lote,no,estab,fno,iva,tabiva,ecomissao,extracom,eextracom,nmdoc,vendedor,vendnm,zona,tipo,pago,pdata,usr1,usr2,usr3,usr4,usr5,familia,pcusto,tcusto,epcusto,etcusto,cpoc,pctdesc,area) select ft.classe, case when inserted.mhstamp <> SPACE(25) then '' when ft.snstamp = '' then inserted.snstamp  else ft.snstamp end ,inserted.fistamp,inserted.fistamp,inserted.ftstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',case when td.lifref=1 then inserted.fifref else ft.fref end,case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,ft.nome,ft.segmento,ft.fdata,inserted.design,case when inserted.ref<>'' then inserted.ref else inserted.oref end,(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),case when inserted.iectin<>0 then round(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 1) end,case when inserted.iectin<>0 then round(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) end,case when inserted.eiectin<>0 then round(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 2) end,case when inserted.eiectin<>0 then round(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) end,round((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),round((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),round(case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),round(case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),inserted.num1,inserted.texteis,inserted.cor,inserted.tam,inserted.usalote,inserted.lote,ft.no,ft.estab,ft.fno,inserted.iva,inserted.tabiva,inserted.ecomissao,inserted.extracom,inserted.eextracom,ft.nmdoc,case when td.LIVENDEDOR=1 then inserted.fivendedor else ft.vendedor end,case when td.LIVENDEDOR=1 then inserted.fivendnm else ft.vendnm end,ft.zona,ft.tipo,case when td.lancacc=1 and not td.ccautoreg=1 then 0 else 1 end,case when td.lancacc=1 and not td.ccautoreg=1 then  convert(datetime,'19000101') else ft.fdata end,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.familia,inserted.custo,inserted.custo*(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),inserted.ecusto,inserted.ecusto*(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),inserted.cpoc,inserted.desconto,ft2.area from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.coms=1 and inserted.qtt<>0 and inserted.composto=0 SET Context_Info 0x55555 UPDATE FT SET  ft.custo=ft.custo+(select case when inserted.tipodoc=3 then sum(-inserted.qtt*inserted.custo) else sum(inserted.qtt*inserted.custo) end  from inserted where inserted.ftstamp=ft.ftstamp and inserted.composto=0 and ft.anulado = 0 group by inserted.tipodoc) ,  ft.ecusto=ft.ecusto+(select case when inserted.tipodoc=3 then sum(-inserted.qtt*inserted.ecusto) else sum(inserted.qtt*inserted.ecusto) end  from inserted where inserted.ftstamp=ft.ftstamp and inserted.composto=0 and ft.anulado = 0 group by inserted.tipodoc)  from inserted where inserted.ftstamp=ft.ftstamp and inserted.composto=0 and ft.anulado = 0  SET Context_Info 0x66666 IF (select count(*) from inserted inner join ft on inserted.ftstamp=ft.ftstamp where ft.anulado = 1) > 0 UPDATE ft SET ft.anulado = (SELECT ft.anulado FROM ft INNER JOIN inserted ON ft.ftstamp = inserted.ftstamp) FROM ft inner join inserted on inserted.ftstamp=ft.ftstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_boma_insert ON boma WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (select count(*) from inserted where inserted.emconf=0)>0 BEGIN IF (select count(*) from inserted where inserted.trfarm=1)>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.noarm=inserted.ar2mazem,ma.armdatain=inserted.dataobra, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.trfarm=1 and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0)>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnome,flno,flestab,flnmdoc,fladoc,fldata,obs,foorigem,situacao) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nome,inserted.no,inserted.estab, inserted.nmdos,inserted.obrano,inserted.dataobra, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end,'BO',inserted.situacao from inserted inner join bo on inserted.bostamp=bo.bostamp where not exists (select bomastamp from ma where ma.bomastamp=inserted.bomastamp) and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=1)>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=1,ma.retfor=0,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra,  ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=1 and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.indisponivel=1 from inserted where inserted.ctrmastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0  and not inserted.ctrmastamp='' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.nome=ma2.nome,ma.no=ma2.no,ma.morada=ma2.morada,ma.local=ma2.local,ma.codpost=ma2.codpost,ma.zona=ma2.zona,ma.estab=ma2.estab,ma.vendnm=ma2.vendnm,ma.vendedor=ma2.vendedor,ma.ftorigem='BO',ma.instal=ma2.instal, ma.telefone=ma2.telefone,ma.contacto=ma2.contacto,ma.email=ma2.email, ma.ftnmdoc=inserted.nmdos,ma.ftfno=inserted.obrano,ma.ftfdata=inserted.dataobra,ma.bostampv=inserted.bostamp,ma.obs=case when inserted.obs='' then ma2.obs else substring(ma2.obs,1,datalength(ma2.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join ma on ma.mastamp=inserted.bomastamp inner join ma ma2 on ma2.mastamp=inserted.ctrmastamp where ma2.mastamp=inserted.ctrmastamp and ma.mastamp=inserted.bomastamp and ma2.no>0 and inserted.foserie=1 and inserted.bdemp='FL' and inserted.retequi=0 and not inserted.ctrmastamp='' and inserted.emconf=0 END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome=inserted.nome,ma.no=inserted.no,ma.bostampv=inserted.bostamp, ma.casa=0, ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.estab=bo.estab,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor,ma.ftorigem='BO',ma.instal=bo.dataobra, ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra, ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where inserted.mastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=1,ma.noarm=inserted.armazem,ma.armdatain=inserted.dataobra, ma.nome=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then inserted.nome else ma.nome end,ma.no=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then inserted.no else ma.no end,ma.estab=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.estab else ma.estab end,ma.morada=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.morada else ma.morada end,ma.local=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.local else ma.local end,ma.codpost=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.codpost else ma.codpost end,ma.zona=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then bo.zona else ma.zona end,ma.telefone=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.telefone else ma.telefone end,ma.contacto=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.contacto else ma.contacto end,ma.email=case when ISNULL(ts2.ocliequi,0)=1 And inserted.ocliequi=1 then cl.email else ma.email end,ma.obs=case when ISNULL(ts2.ocliequi,0)=1 And inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos inner join ts2 on ts.tsstamp=ts2.ts2stamp where inserted.mastamp=ma.mastamp and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,nome,no,morada,local,codpost,zona,estab,vendnm,vendedor,instal,telefone,contacto,email,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao, inserted.nome,inserted.no,bo.morada,bo.local,bo.codpost,bo.zona,bo.estab,bo.vendnm,bo.vendedor,bo.dataobra,cl.telefone,cl.contacto,cl.email,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='CL' and not inserted.ctrmastamp='')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome='',ma.no=0,ma.morada='',ma.local='',ma.codpost='',ma.zona='',ma.estab=0, ma.vendnm='',ma.vendedor=0,ma.telefone='',ma.email='',ma.contacto='' from inserted where inserted.ctrmastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='CL' and not inserted.ctrmastamp='' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL')>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,nome,no,morada,local,codpost,zona,estab,vendnm,vendedor,instal,telefone,contacto,email,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao,case when ts.actprodclp=1 then inserted.nome else '' end,case when ts.actprodclp=1 then inserted.no else 0 end,case when ts.actprodclp=1 then bo.morada else '' end,case when ts.actprodclp=1 then bo.local else '' end,case when ts.actprodclp=1 then bo.codpost else '' end,case when ts.actprodclp=1 then bo.zona else '' end,case when ts.actprodclp=1 then bo.estab else 0 end,case when ts.actprodclp=1 then bo.vendnm else '' end,case when ts.actprodclp=1 then bo.vendedor else 0 end,case when ts.actprodclp=1 then bo.dataobra else '' end,case when ts.actprodclp=1 then cl.telefone else '' end,case when ts.actprodclp=1 then cl.contacto else '' end,case when ts.actprodclp=1 then cl.email else '' end,case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and (inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp='CL') and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp='CL')>0 BEGIN UPDATE MA SET ma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='BO',ma.bostampv=inserted.bostamp,ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra,ma.nome=case when ts.actprodclc=1 then inserted.nome else '' end,ma.no=case when ts.actprodclc=1 then inserted.no else 0 end,ma.morada=case when ts.actprodclc=1 then bo.morada else '' end,ma.local=case when ts.actprodclc=1 then bo.local else '' end,ma.codpost=case when ts.actprodclc=1 then bo.codpost else '' end,ma.zona=case when ts.actprodclc=1 then bo.zona else '' end,ma.estab=case when ts.actprodclc=1 then bo.estab else 0 end,ma.vendnm=case when ts.actprodclc=1 then bo.vendnm else '' end,ma.vendedor=case when ts.actprodclc=1 then bo.vendedor else 0 end,ma.instal=case when ts.actprodclc=1 then bo.dataobra else '' end,ma.telefone=case when ts.actprodclc=1 then cl.telefone else '' end,ma.contacto=case when ts.actprodclc=1 then cl.contacto else '' end,ma.email=case when ts.actprodclc=1 then cl.email else '' end,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab inner join ts on bo.ndos=ts.ndos where inserted.mastamp=ma.mastamp and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp='CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL')>0 BEGIN INSERT INTO MA (mastamp,bomastamp,bostamp,bostampc,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,serie,serie2,marca,maquina,tipo,ref,design,fref,ccusto,ncusto,casa,noarm,armdatain,user1,user2,user3,user4,flnmdoc,fladoc,fldata,foorigem,situacao,obs) select inserted.bomastamp,inserted.bomastamp,inserted.bostamp,inserted.bostamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,inserted.serie,inserted.serie2,inserted.marca,inserted.maquina,inserted.tipo,inserted.ref,inserted.design,inserted.fref,inserted.ccusto,inserted.ncusto,1,inserted.armazem,inserted.dataobra,inserted.user1,inserted.user2,inserted.user3,inserted.user4,inserted.nmdos,inserted.obrano,inserted.dataobra,'BO',inserted.situacao, case when inserted.obs='' then '' else inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp where not exists (select mastamp from ma where ma.mastamp=inserted.mastamp) and (inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=1 and inserted.refprod=1 and inserted.bdemp<>'CL') and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp<>'CL')>0 BEGIN UPDATE MA SET ma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.casa=0,ma.indisponivel=1,ma.ftorigem='BO',ma.bostampv=inserted.bostamp,ma.ftnmdoc=bo.nmdos,ma.ftfno=bo.obrano,ma.ftfdata=bo.dataobra, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.foserie=0 and inserted.ftserie=0 and inserted.trfarm=0 and inserted.producao=0 and inserted.refprod=1 and inserted.bdemp<>'CL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.ftserie=1 and inserted.bdemp='FL')>0 BEGIN UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.ftnmdoc=inserted.nmdos,ma.ftfno=inserted.obrano,ma.ftfdata=inserted.dataobra,ma.bostampv=inserted.bostamp,ma.ftstamp='BO',ma.casa=0,ma.retfor=1,ma.armfornout=inserted.dataobra,ma.flnomea=inserted.nome,ma.flnoa=inserted.no,ma.flestaba=inserted.estab, ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.dataobra,121)+')' end from inserted inner join bo on inserted.bostamp=bo.bostamp where inserted.mastamp=ma.mastamp and inserted.ftserie=1 and inserted.bdemp='FL' and inserted.emconf=0END IF (select count(*) from inserted where inserted.foserie=1 and inserted.bdemp='CL' and inserted.ocliequi=1)>0 and exists (select boma.ocliequi from boma,inserted where boma.bostamp=inserted.bostamp And boma.bistamp=inserted.bistamp And boma.ocliequi=1) BEGIN UPDATE BOMA SET boma.ocliequi=0 from inserted where inserted.bostamp=boma.bostamp And inserted.bistamp=boma.bistamp and inserted.foserie=1 and inserted.bdemp='CL' and inserted.emconf=0 And exists (select boma.ocliequi from boma where boma.bostamp=inserted.bostamp And boma.bistamp=inserted.bistamp And boma.ocliequi=1)END IF (select COUNT(*) from inserted where inserted.serprvef = 1) > 0 UPDATE MA SET ma.serie = inserted.serie from inserted where inserted.serprvef = 1 and inserted.mastamp=ma.mastamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftma_update ON ftma WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.nome=case when deleted.tipodoc=3 then ft.nome else '' end,ma.no=case when deleted.tipodoc=3 then ft.no else 0 end,ma.ftnmdoc=case when deleted.tipodoc=3 then ma.ftnmdoc else '' end,ma.ftfno=case when deleted.tipodoc=3 then ma.ftfno else 0 end,ma.ftfdata=case when deleted.tipodoc=3 then ma.ftfdata else '' end,ma.ftstamp=case when deleted.tipodoc=3 then ma.ftstamp else '' end,ma.casa=case when deleted.tipodoc=3 then 0 else 1 end,ma.morada=case when deleted.tipodoc=3 then ft.morada else '' end,ma.local=case when deleted.tipodoc=3 then ft.local else '' end,ma.codpost=case when deleted.tipodoc=3 then ft.codpost else '' end,ma.zona=case when deleted.tipodoc=3 then ft.zona else '' end,ma.vendnm=case when deleted.tipodoc=3 then ft.vendnm else '' end,ma.vendedor=case when deleted.tipodoc=3 then ft.vendedor else 0 end,ma.instal=case when deleted.tipodoc=3 then ma.instal else '' end,ma.telefone=case when deleted.tipodoc=3 then cl.telefone else '' end,ma.contacto=case when deleted.tipodoc=3 then cl.contacto else '' end,ma.estab=case when deleted.tipodoc=3 then ft.estab else 0 end,ma.email=case when deleted.tipodoc=3 then cl.email else '' end,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.fdata,121))+4,datalength(ma.obs)), ma.ftorigem=case when deleted.tipodoc=3 then ma.ftorigem else ''  end from deleted inner join ma on deleted.mastamp=ma.mastamp inner join ft on ma.ftstamp=ft.ftstamp inner join cl on cl.no=ft.no and cl.estab=ft.estab UPDATE MA SETma.usrinis=inserted.usrinis,ma.usrhora=inserted.usrhora,ma.usrdata=inserted.usrdata,ma.nome=case when ft.tipodoc=3 then '' else inserted.nome end,ma.no=case when ft.tipodoc=3 then 0 else inserted.no end,ma.ftnmdoc=case when ft.tipodoc=3 then ma.ftnmdoc else inserted.nmdoc end,ma.ftfno=case when ft.tipodoc=3 then ma.ftfno else inserted.fno end ,ma.ftfdata=case when ft.tipodoc=3 then ma.ftfdata else inserted.fdata end, ma.ftstamp=case when ft.tipodoc=3 then ma.ftstamp else inserted.ftstamp end,ma.casa=case when ft.tipodoc=3 then 1 else 0 end,ma.morada=case when ft.tipodoc=3 then '' else ft.morada end,ma.local=case when ft.tipodoc=3 then '' else ft.local end,ma.codpost=case when ft.tipodoc=3 then '' else ft.codpost end,ma.zona=case when ft.tipodoc=3 then '' else ft.zona end,ma.vendnm=case when ft.tipodoc=3 then '' else ft.vendnm end,ma.vendedor=case when ft.tipodoc=3 then 0 else ft.vendedor end,ma.instal=case when ft.tipodoc=3 then ma.instal else ft.fdata end,ma.telefone=case when ft.tipodoc=3 then '' else cl.telefone end,ma.contacto=case when ft.tipodoc=3 then '' else cl.contacto end,ma.estab=case when ft.tipodoc=3 then 0 else ft.estab end,ma.email=case when ft.tipodoc=3 then '' else cl.email end,ma.obs=case when inserted.obs='' then ma.obs else substring(ma.obs,1,datalength(ma.obs))+char(10)+inserted.obs+SPACE(1)+'('+convert(char(10),inserted.fdata,121)+')'  end, ma.ftorigem=case when ft.tipodoc=3 then ma.ftorigem else 'FT' end, ma.noarm=case when ft.tipodoc=3 then inserted.armazem else ma.noarm end, ma.armdatain=case when ft.tipodoc=3 then inserted.fdata else ma.armdatain end from inserted inner join ft on inserted.ftstamp=ft.ftstamp inner join cl on cl.no=ft.no and cl.estab=ft.estab where inserted.mastamp=ma.mastamp  if UPDATE(serprvef) BEGIN IF (select COUNT(*) from deleted where deleted.serprvef = 1) > 0 UPDATE MA SET ma.serie = deleted.maserierpl from deleted where deleted.serprvef = 1 and deleted.mastamp=ma.mastamp IF (select COUNT(*) from inserted where inserted.serprvef = 1) > 0 UPDATE MA SET ma.serie = inserted.serie from inserted where inserted.serprvef = 1 and inserted.mastamp=ma.mastamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cc_update ON CC WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONdeclare @m_banchab char(60), @m_contado float select @m_banchab = ISNULL((select valor from para1 where descricao='m_banchab'),'') SELECT @m_contado = ISNULL((SELECT TOP 1 noconta FROM bl WHERE bl.banco = SUBSTRING(@m_banchab, 1, 10) AND bl.conta = SUBSTRING(@m_banchab, 12, 20)),0) if UPDATE(deb) or UPDATE(cred) or UPDATE(edeb) or UPDATE(ecred) or UPDATE(no) UPDATE CL SET cl.saldo=cl.saldo-(select Sum(deleted.deb-deleted.cred+deleted.difcambio) from deleted where deleted.no=cl.no), cl.esaldo=cl.esaldo-(select Sum(deleted.edeb-deleted.ecred+deleted.edifcambio) from deleted where deleted.no=cl.no) , cl.pcktsyncdate = convert(datetime, substring(convert(char,getdate()),1,12)), cl.pcktsynctime = CONVERT(CHAR(8),GETDATE(),8) from DELETED where cl.no=deleted.no if UPDATE(deb) or UPDATE(cred) or UPDATE(edeb) or UPDATE(ecred) or UPDATE(no) UPDATE CL SET cl.saldo=cl.saldo+(select Sum(inserted.deb-inserted.cred+inserted.difcambio) from inserted where inserted.no=cl.no), cl.esaldo=cl.esaldo+(select Sum(inserted.edeb-inserted.ecred+inserted.edifcambio) from inserted where inserted.no=cl.no) , cl.pcktsyncdate = convert(datetime, substring(convert(char,getdate()),1,12)), cl.pcktsynctime = CONVERT(CHAR(8),GETDATE(),8)   from INSERTED where cl.no=inserted.no if (select count(*) from deleted where RTRIM(LTRIM(deleted.occstamp)) = '') > 0 and (UPDATE(no) or UPDATE(deb) or UPDATE(cred) or UPDATE(edeb) or UPDATE(ecred) or UPDATE(cr) or UPDATE(docref)) UPDATE CC SET cc.debf = cc.debf-deleted.cred, cc.credf = cc.credf-deleted.deb, cc.edebf = cc.edebf-deleted.ecred, cc.ecredf = cc.ecredf-deleted.edeb from deleted where cc.cm=deleted.cr and cc.nrdoc=deleted.docref and cc.no=deleted.no and deleted.origem='CC' if (select count(*) from deleted where RTRIM(LTRIM(deleted.occstamp)) <> '') > 0 and (UPDATE(no) or UPDATE(deb) or UPDATE(cred) or UPDATE(edeb) or UPDATE(ecred) or UPDATE(cr) or UPDATE(docref)) UPDATE CC SET cc.debf = cc.debf-deleted.cred, cc.credf = cc.credf-deleted.deb, cc.edebf = cc.edebf-deleted.ecred, cc.ecredf = cc.ecredf-deleted.edeb from deleted where cc.cm=deleted.cr and cc.nrdoc=deleted.docref and cc.no=deleted.no and deleted.origem='CC' and cc.ccstamp = deleted.occstampif (select count(*) from inserted where RTRIM(LTRIM(inserted.occstamp)) = '') > 0 and (UPDATE(no) or UPDATE(deb) or UPDATE(cred) or UPDATE(edeb) or UPDATE(ecred) or UPDATE(cr) or UPDATE(docref)) UPDATE CC SET cc.debf = cc.debf+inserted.cred, cc.credf = cc.credf+inserted.deb, cc.edebf = cc.edebf+inserted.ecred, cc.ecredf = cc.ecredf+inserted.edeb from INSERTED where cc.cm=inserted.cr and cc.nrdoc=inserted.docref and cc.no=inserted.no and inserted.origem='CC' if (select count(*) from inserted where RTRIM(LTRIM(inserted.occstamp)) <> '') > 0 and (UPDATE(no) or UPDATE(deb) or UPDATE(cred) or UPDATE(edeb) or UPDATE(ecred) or UPDATE(cr) or UPDATE(docref)) UPDATE CC SET cc.debf = cc.debf+inserted.cred, cc.credf = cc.credf+inserted.deb, cc.edebf = cc.edebf+inserted.ecred, cc.ecredf = cc.ecredf+inserted.edeb from INSERTED where cc.cm=inserted.cr and cc.nrdoc=inserted.docref and cc.no=inserted.no and inserted.origem='CC' and cc.ccstamp = inserted.occstampDELETE FROM OD FROM deleted where deleted.ccstamp=od.odstamp INSERT INTO OD (ccstamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,entr,eentr,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local,moeda,boacobranca) select inserted.ccstamp,inserted.ccstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'CC',inserted.cmdesc,ltrim(str(inserted.nrdoc,10,0)),dateadd(day,cl.odatraso,inserted.dataven) ,(inserted.deb-inserted.debf)-(inserted.cred-inserted.credf) 												- (inserted.ivacativado - abs(inserted.ivacatreg)),(inserted.edeb-inserted.edebf)-(inserted.ecred-inserted.ecredf) 												- (inserted.eivacativado - abs(inserted.eivacatreg)),'Rec :'+inserted.nome,cm1.olcl,cm1.clgrupo,cm1.clsgrupo,inserted.fref,inserted.ccusto,inserted.ncusto , case when isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0 then @m_banchab else isnull(ft2.vdollocal,@m_banchab) end as contam_banchab  ,case when isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0  then @m_contado else ft2.vdcontado end as contacontado    ,case when NOT(isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0) then ft2.vdlocal   else case when @m_contado>0 then 'B' else 'C' end end as contalocal    ,case when isnull(ft2.vdollocal,@m_banchab)='' or isnull(td.altollocal,0)=0 then 'PTE ou EURO' else ft2.olmoeda end as contaolmoeda  ,case when isnull(cl.cobnao,0) = 0 then cm1.boacobranca else 0 end as boacob from inserted inner join cl on cl.no=inserted.no and cl.estab = inserted.estab inner join cm1 on cm1.cm=inserted.cm   left join ft2 on ft2.ft2stamp = inserted.ftstamp  left join ft on ft.ftstamp = ft2.ft2stamp  left join td on td.ndoc=ft.ndoc  where ((inserted.edeb-inserted.edebf)-(inserted.ecred-inserted.ecredf)<>0 and (inserted.deb-inserted.debf)-(inserted.cred-inserted.credf)<>0) and inserted.incobra=0 if UPDATE (debf) or UPDATE(credf) or UPDATE(edebf) or UPDATE(ecredf)  UPDATE PN SET pn.pago=0 from deleted where deleted.ftstamp>space(25) and ((deleted.deb-deleted.debf)-(deleted.cred-deleted.credf)=0 or (deleted.edeb-deleted.edebf)-(deleted.ecred-deleted.ecredf)=0) and pn.ftstamp=deleted.ftstamp and (select td.ccautoreg from td inner join ft on td.ndoc=ft.ndoc where ft.ftstamp=pn.ftstamp)<>1if UPDATE (debf) or UPDATE(credf) or UPDATE(edebf) or UPDATE(ecredf)    UPDATE PN SET  pn.pago=ISNULL((select top 1 case when tsre.movinc = 0 then 1 else 0 end as pago   from tsre (nolock) inner join re (nolock) on re.ndoc = tsre.ndoc   inner join rl (nolock) on re.restamp = rl.restamp   where rl.ccstamp = inserted.ccstamp order by re.procdata, rl.ousrhora desc),0),pn.pdata=isnull((select top 1 re.procdata from rl (nolock) left join re (nolock) on rl.restamp=re.restamp where rl.ccstamp=inserted.ccstamp order by re.procdata desc),isnull((select top 1 le.data from li (nolock) left join le (nolock) on li.lestamp=le.lestamp where li.ccstamp=inserted.ccstamp order by le.data desc),getdate())) from INSERTED where inserted.ftstamp>space(25) and ((inserted.deb-inserted.debf)-(inserted.cred-inserted.credf)=0 or (inserted.edeb-inserted.edebf)-(inserted.ecred-inserted.ecredf)=0) and pn.ftstamp=inserted.ftstamp and (select td.ccautoreg from td inner join ft on td.ndoc=ft.ndoc where ft.ftstamp=pn.ftstamp)<>1GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fn_update ON fn WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128) SELECT @Cinfo = Context_Info() IF @Cinfo = 0x88888 RETURN DELETE FROM SL FROM inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where (sl.fnstamp=inserted.fnstamp and inserted.fnstamp<>'') and (inserted.ref='' or cm1.folansl=0)DELETE FROM SV FROM inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where (sv.fnstamp=inserted.fnstamp and inserted.fnstamp<>'') and (inserted.ref='' or (cm1.folansv=0 and cm1.folansvgr = 0))if update(usrdata) or update(usrhora) or update(usrinis) or update(fnfref) or update(codigo) or update(design) or update(ref) or update(lote) or update(armazem) or update(qtt) or update(sltt) or update(slvu) or update(esltt) or update(eslvu) or update(composto) or update(cor) or update(tam) or update(slvumoeda) or update(cpoc) or update(ivaincl) or update(tiliquido) or update(etiliquido) or update(iva) or update(fostamp) or update(usr1) or update(usr2) or update(usr3) or update(adoc) or update(data) or update(fnfref) or update(fnccusto) or update(fnncusto) or update(fnstamp)  or update(usr4) or update(docnome) or update(stns) BEGINUPDATE SL set sl.usrdata=inserted.usrdata,sl.usrhora=inserted.usrhora,sl.usrinis=inserted.usrinis,sl.fref=fo.fref,sl.ccusto=inserted.fnccusto,sl.ncusto=fo.ncusto,sl.nome=fo.nome,sl.codigo=inserted.codigo,sl.moeda=fo.moeda,sl.datalc=(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),sl.design=inserted.design,sl.ref=inserted.ref,sl.lote=inserted.lote,sl.armazem=inserted.armazem,sl.qtt=inserted.qtt,sl.num1=inserted.num1,sl.tt=inserted.sltt,sl.vu=inserted.slvu,sl.ett=inserted.esltt,sl.evu=inserted.eslvu,sl.frcl=fo.no,sl.adoc=fo.adoc,sl.cm=cm1.fosl,sl.cmdesc=cm1.fosln,sl.composto=inserted.composto,sl.cor=inserted.cor,sl.tam=inserted.tam,sl.usr1=inserted.usr1,sl.usr2=inserted.usr2,sl.usr3=inserted.usr3,sl.usr4=inserted.usr4,sl.vumoeda=inserted.slvumoeda,sl.ttmoeda=inserted.slvumoeda*inserted.qtt,sl.pcpond=inserted.slvu,sl.cpoc=inserted.cpoc,sl.valiva=case when inserted.ivaincl=1 then inserted.tiliquido-inserted.tiliquido/(1+inserted.iva/100) else inserted.tiliquido*inserted.iva/100 end,sl.evaliva=case when inserted.ivaincl=1 then inserted.etiliquido-inserted.etiliquido/(1+inserted.iva/100) else inserted.etiliquido*inserted.iva/100 end,sl.epcpond=inserted.eslvu,sl.stns=inserted.stns from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1  on fo.doccode=cm1.cm  where inserted.fnstamp=sl.fnstamp and cm1.folansl=1  ENDif update(fnstamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrhora) or update(ousrinis) or update(ousrdata) or update(codigo) or update(design) or update(ref) or update(lote) or update(armazem) or update(qtt) or update(sltt) or update(slvu) or update(esltt) or update(eslvu) or update(composto) or update(cor) or update(tam) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(slvumoeda) or update(cpoc) or update(ivaincl) or update(tiliquido) or update(iva) or update(etiliquido) or update(fostamp) or update(adoc) or update(data) or update(fnfref) or update(fnccusto) or update(fnncusto) or update(fnstamp) or update(fnfref) or update(docnome) or update(stns)BEGININSERT INTO SL (slstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,ref,lote,armazem,qtt,tt,vu,ett,evu,num1,frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,vumoeda,ttmoeda,pcpond,cpoc,valiva,evaliva,epcpond,stns )select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.fref,inserted.fnccusto,fo.ncusto,fo.nome,inserted.codigo,fo.moeda,(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.armazem,inserted.qtt,inserted.sltt,inserted.slvu,inserted.esltt,inserted.eslvu,inserted.num1,fo.no,fo.adoc,cm1.fosl,cm1.fosln,inserted.composto,inserted.cor,inserted.tam,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.slvumoeda,inserted.slvumoeda*inserted.qtt,inserted.slvu,inserted.cpoc,case when inserted.ivaincl=1 then inserted.tiliquido-inserted.tiliquido/(1+inserted.iva/100) else inserted.tiliquido*inserted.iva/100 end,case when inserted.ivaincl=1 then inserted.etiliquido-inserted.etiliquido/(1+inserted.iva/100) else inserted.etiliquido*inserted.iva/100 end,inserted.eslvu,inserted.stns from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm inner join fo2 on fo.fostamp=fo2.fo2stamp and fo2.anulado=0 where not EXISTS(select slstamp from sl where sl.slstamp=inserted.fnstamp) and cm1.folansl=1  and inserted.ref<>space(18) ENDif update(usrdata) or update(usrhora) or update(usrinis) or update(design) or update(ref) or update(cor) or update(lote) or update(tiliquido) or update(tam) or update(armazem) or update(fostamp) or update(etiliquido) or update(tmoeda ) or update(adoc) or update(data) or update(fnstamp) or update(docnome) BEGINUPDATE SV set sv.usrdata=inserted.usrdata,sv.usrhora=inserted.usrhora,sv.usrinis=inserted.usrinis,sv.nome=fo.nome,sv.moeda=fo.moeda,sv.data=(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),sv.design=inserted.design,sv.ref=inserted.ref,sv.lote=inserted.lote,sv.cor=inserted.cor,sv.tam=inserted.tam,sv.armazem=inserted.armazem,sv.tt=case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0) then inserted.sltt else inserted.tiliquido end,sv.ett=case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0) then inserted.esltt else inserted.etiliquido end,sv.no=fo.no,sv.adoc=fo.adoc,sv.cm=cm1.fosv,sv.cmdesc=cm1.fosvn,sv.ttmoeda=inserted.tmoeda from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1  on fo.doccode=cm1.cm  where inserted.fnstamp=sv.fnstamp and cm1.folansv=1 and cm1.folansvgr = 0 ENDif (update(usrdata) or update(usrhora) or update(usrinis) or update(design) or update(ref) or update(cor) or update(lote) or update(tiliquido) or update(tam) or update(armazem) or update(fostamp) or update(etiliquido) or update(tmoeda ) or update(adoc) or update(data) or update(fnstamp) or update(docnome)) and (select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.bistamp <> '' and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) )>0 BEGINUPDATE SV set sv.usrdata=inserted.usrdata,sv.usrhora=inserted.usrhora,sv.usrinis=inserted.usrinis,sv.nome=fo.nome,sv.moeda='PTE ou EURO',sv.data=(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),sv.design=inserted.design,sv.ref=inserted.ref,sv.lote=inserted.lote,sv.cor=inserted.cor,sv.tam=inserted.tam,sv.armazem=inserted.armazem,sv.tt = case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0)  then round(inserted.sltt - (SELECT (case when bi.qtt = 0 then bi.debito else bi.ttdeb end) from bi where bi.bistamp = inserted.bistamp), 1) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.pv else inserted.tiliquido / inserted.qtt end) - (SELECT (case when bi.qtt = 0 then bi.debito else bi.ttdeb / bi.qtt end) from bi where bi.bistamp = inserted.bistamp)),1)end, sv.ett = case when (fo.evalacres > 0 or fo.edespinc > 0 or inserted.evalacres > 0)  then round(inserted.esltt - (SELECT (case when bi.qtt = 0 then bi.edebito else bi.ettdeb end) from bi where bi.bistamp = inserted.bistamp), 2) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.epv else inserted.etiliquido / inserted.qtt end) - (SELECT (case when bi.qtt = 0 then bi.edebito else bi.ettdeb / bi.qtt end) from bi where bi.bistamp = inserted.bistamp)),2)end, sv.no=fo.no,sv.adoc=fo.adoc,sv.cm=cm1.fosv,sv.cmdesc=cm1.fosvn from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1  on fo.doccode=cm1.cm where inserted.fnstamp=sv.fnstamp and cm1.folansv=0 and cm1.folansvgr = 1 ENDif (update(usrdata) or update(usrhora) or update(usrinis) or update(design) or update(ref) or update(cor) or update(lote) or update(tiliquido) or update(tam) or update(armazem) or update(fostamp) or update(etiliquido) or update(tmoeda ) or update(adoc) or update(data) or update(fnstamp) or update(docnome)) and (select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.ofnstamp <> '' and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) )>0 BEGINUPDATE SV set sv.usrdata=inserted.usrdata,sv.usrhora=inserted.usrhora,sv.usrinis=inserted.usrinis,sv.nome=fo.nome,sv.moeda='PTE ou EURO',sv.data=(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),sv.design=inserted.design,sv.ref=inserted.ref,sv.lote=inserted.lote,sv.cor=inserted.cor,sv.tam=inserted.tam,sv.armazem=inserted.armazem,sv.tt = case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0)  then round(inserted.sltt - (SELECT(case when fn.qtt = 0 then fn.pv else fn.tiliquido end) from fn where fn.fnstamp = inserted.ofnstamp), 1) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.pv else inserted.tiliquido / inserted.qtt end) - (SELECT(case when fn.qtt = 0 then fn.pv else fn.tiliquido / fn.qtt end) from fn where fn.fnstamp = inserted.ofnstamp)), 1)end, sv.ett = case when (fo.evalacres > 0 or fo.edespinc > 0 or inserted.evalacres > 0)  then round(inserted.esltt - (SELECT(case when fn.qtt = 0 then fn.epv else fn.etiliquido end) from fn where fn.fnstamp = inserted.ofnstamp), 2) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.epv else inserted.etiliquido / inserted.qtt end) - (SELECT(case when fn.qtt = 0 then fn.epv else fn.etiliquido / fn.qtt end) from fn where fn.fnstamp = inserted.ofnstamp)), 2)end, sv.no=fo.no,sv.adoc=fo.adoc,sv.cm=cm1.fosv,sv.cmdesc=cm1.fosvn from inserted inner join fo on inserted.fostamp=fo.fostamp inner join cm1  on fo.doccode=cm1.cm  where inserted.fnstamp=sv.fnstamp and cm1.folansv=0 and cm1.folansvgr = 1 ENDif update(fnstamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrinis) or update(ousrhora) or update(ousrdata) or update(design) or update(ref) or update(lote) or update(cor) or update(tam) or update(armazem) or update(tiliquido) or update(etiliquido) or update(adoc) or update(data) or update(tmoeda ) or update(fostamp ) or update(fnstamp) or update(docnome) BEGININSERT INTO SV (svstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc,ttmoeda )select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.nome,fo.moeda,(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem,case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0) then inserted.sltt else  inserted.tiliquido end,case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0) then inserted.esltt else inserted.etiliquido end,fo.no,fo.adoc,cm1.fosv,cm1.fosvn,inserted.tmoeda from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm  where not EXISTS(select svstamp from sv where sv.svstamp=inserted.fnstamp) and cm1.folansv=1 and cm1.folansvgr = 0 and inserted.ref<>space(18) ENDif (update(fnstamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrinis) or update(ousrhora) or update(ousrdata) or update(design) or update(ref) or update(lote) or update(cor) or update(tam) or update(armazem) or update(tiliquido) or update(etiliquido) or update(adoc) or update(data) or update(fostamp ) or update(fnstamp) or update(docnome)) and (select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.bistamp <> '' and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) )>0 BEGININSERT INTO SV (svstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.nome,'PTE ou EURO',(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem, case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0)  then round(inserted.sltt - (SELECT (case when bi.qtt = 0 then bi.debito else bi.ttdeb end) from bi where bi.bistamp = inserted.bistamp), 1) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.pv else inserted.tiliquido / inserted.qtt end) - (SELECT (case when bi.qtt = 0 then bi.debito else bi.ttdeb / bi.qtt end) from bi where bi.bistamp = inserted.bistamp)),1)end,  case when (fo.evalacres > 0 or fo.edespinc > 0 or inserted.evalacres > 0)  then round(inserted.esltt - (SELECT (case when bi.qtt = 0 then bi.edebito else bi.ettdeb end) from bi where bi.bistamp = inserted.bistamp), 2) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.epv else inserted.etiliquido / inserted.qtt end) - (SELECT (case when bi.qtt = 0 then bi.edebito else bi.ettdeb / bi.qtt end) from bi where bi.bistamp = inserted.bistamp)),2)end, fo.no,fo.adoc,cm1.fosv,cm1.fosvn from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where not EXISTS(select svstamp from sv where sv.svstamp=inserted.fnstamp) and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) ENDif (update(fnstamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrinis) or update(ousrhora) or update(ousrdata) or update(design) or update(ref) or update(lote) or update(cor) or update(tam) or update(armazem) or update(tiliquido) or update(etiliquido) or update(adoc) or update(data) or update(fostamp ) or update(fnstamp) or update(docnome)) and (select count(*) from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where inserted.ofnstamp <> '' and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) )>0 BEGININSERT INTO SV (svstamp,fnstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fnstamp,inserted.fnstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FO',fo.nome,'PTE ou EURO',(case when inserted.svdata > '19000101' then inserted.svdata else fo.data end),inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem, case when (fo.valacres > 0 or fo.despinc > 0 or inserted.valacres > 0)  then round(inserted.sltt - (SELECT(case when fn.qtt = 0 then fn.pv else fn.tiliquido end) from fn where fn.fnstamp = inserted.ofnstamp), 1) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.pv else inserted.tiliquido / inserted.qtt end) - (SELECT(case when fn.qtt = 0 then fn.pv else fn.tiliquido / fn.qtt end) from fn where fn.fnstamp = inserted.ofnstamp)), 1)end,  case when (fo.evalacres > 0 or fo.edespinc > 0 or inserted.evalacres > 0)  then round(inserted.esltt - (SELECT(case when fn.qtt = 0 then fn.epv else fn.etiliquido end) from fn where fn.fnstamp = inserted.ofnstamp), 2) else round(inserted.qtt * ((case when inserted.qtt = 0 then inserted.epv else inserted.etiliquido / inserted.qtt end) - (SELECT(case when fn.qtt = 0 then fn.epv else fn.etiliquido / fn.qtt end) from fn where fn.fnstamp = inserted.ofnstamp)), 2)end, fo.no,fo.adoc,cm1.fosv,cm1.fosvn from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm where not EXISTS(select svstamp from sv where sv.svstamp=inserted.fnstamp) and cm1.folansv=0 and cm1.folansvgr = 1 and inserted.ref<>space(18) END If (select top 1 cm1.fonactqtt2 from deleted inner join fo on fo.fostamp=deleted.fostamp inner join cm1 on fo.doccode=cm1.cm)=0Beginif update(fostamp) or update(bistamp)  or update(qtt) or update(data) BEGINUPDATE BI SET bi.adoc = '',bi.nmdoc = '',bi.ofostamp = '',bi.ndoc = 0, bi.qtt2 = bi.qtt2-deleted.qtt  from deleted inner join fo on deleted.fostamp=fo.fostamp where deleted.bistamp=bi.bistampENDENDIf (select top 1 cm1.fonactqtt2 from inserted inner join fo on fo.fostamp=inserted.fostamp inner join cm1 on fo.doccode=cm1.cm)=0Beginif update(fostamp) or update(bistamp) or update(qtt) or update(data) BEGINUPDATE BI SET bi.fdata = fo.data,bi.adoc = fo.adoc,bi.nmdoc = fo.docnome,bi.ofostamp = fo.fostamp,bi.ndoc = fo.doccode, bi.qtt2 = bi.qtt2+inserted.qtt  from inserted inner join fo on inserted.fostamp=fo.fostamp where inserted.bistamp=bi.bistampENDEND			if (select count(*) from inserted where fnofo<>0 and ndocfo<>0) >0 
			Begin
			UPDATE FO2 SET
			fo2.facturada = 1 , 
			fo2.nmdocfo = (select top 1 fo.docnome from fo where fo.fostamp = i.fostamp),
			fo2.fnofo = (select top 1 fo.no from fo where fo.fostamp = i.fostamp),
			fo2.adocfo = (select top 1 fo.adoc from fo where fo.fostamp = i.fostamp) 
			from inserted i
				inner join fo on 
					fo.doccode = i.ndocfo and fo.no = i.fnofo and fo.foano = i.foanofo and fo.adoc = i.adocfo
			where i.ndocfo<>0 
				and fo2.fo2stamp in (select fo2.fo2stamp from fo2
							inner join fo on fo.fostamp = fo2.fo2stamp
								where fo.doccode = i.ndocfo and fo.no = i.fnofo and fo.foano = i.foanofo and fo.adoc = i.adocfo)
			endGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_pl_update ON pl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONupdate FC
set fc.credf =fc.credf -isnull((select (case when sum(deleted.cambio)=0 and sum(deleted.escrec)>0 then sum(deleted.escrec)- sum(deleted.arred)
else case when sum(deleted.cambio)<>0 and sum(deleted.escrec)>0 then sum(deleted.escrec) else 0 end end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.debf =fc.debf +isnull((select (case when sum(deleted.cambio)=0 and sum(deleted.escrec)<0 then sum(deleted.escrec)- sum(deleted.arred)
else case when sum(deleted.cambio)<>0 and sum(deleted.escrec)<0 then sum(deleted.escrec) else 0 end end)
from deleted where
deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.credfm =fc.credfm -isnull((select (case when sum(deleted.ecambio)<>0 and sum(deleted.rec)>0 then sum(deleted.rec) else 0 end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.debfm =fc.debfm +isnull((select (case when sum(deleted.ecambio)<>0 and sum(deleted.rec)<0 then sum(deleted.rec) else 0 end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.ecredf =fc.ecredf -isnull((select (case when sum(deleted.erec)>0 then sum(deleted.erec)-sum(deleted.earred) else 0 end)
from deleted
where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.edebf =fc.edebf +isnull((select (case when sum(deleted.erec)<0 then sum(deleted.erec)-sum(deleted.earred) else 0 end) from deleted where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.virsreg =fc.virsreg -isnull((select sum(deleted.virs) from deleted where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
, fc.evirsreg =fc.evirsreg -isnull((select sum(deleted.evirs) from deleted where deleted.fcstamp=fc.fcstamp and deleted.process=1),0)
from deleted
where deleted.fcstamp =fc.fcstamp and deleted.process=1

update fc
set fc.evalpo =fc.evalpo - (select sum(deleted.erec -deleted.eivacativado) from deleted where deleted.fcstamp =fc.fcstamp)
, fc.valpo =fc.valpo - (select sum(deleted.escrec -deleted.ivacativado) from deleted where deleted.fcstamp =fc.fcstamp)
, fc.mvalpo =fc.mvalpo - (select sum(deleted.rec -deleted.mivacativado) from deleted where deleted.fcstamp =fc.fcstamp)
, fc.ultdoc =''
, fc.recibado =0
from deleted
where fc.fcstamp =deleted.fcstamp

update FC
set fc.irsdif =fc.irsdif -(select sum(case when deleted.desconto=100 or po.fin=100 then 0
else (round((deleted.virs/(1-(case when deleted.desconto<>0 then deleted.desconto else po.fin end/100))),0)-deleted.virs) end)
from deleted
inner join po on po.postamp =deleted.postamp
where deleted.fcstamp =fc.fcstamp)
, fc.eirsdif =fc.eirsdif -(select sum(case when deleted.desconto=100 or po.fin=100 then 0
else (round((deleted.evirs/(1-(case when deleted.desconto<>0 then deleted.desconto else po.fin end/100))),2)-deleted.evirs) end)
from deleted
inner join po on po.postamp=deleted.postamp
where deleted.fcstamp=fc.fcstamp)
from deleted
inner join po on po.postamp=deleted.postamp
where fc.fcstamp =deleted.fcstamp
and deleted.process=1 and (deleted.desconto<>0 or po.fin<>0)

if ( select count(*) from deleted where deleted.fcorigem='PD')>0
update pd 
set pd.integrado = (case isnull((select top 1 pl.fcstamp 
from pl 
inner join fc on fc.fcstamp =pl.fcstamp 
where fc.fcstamp =deleted.fcstamp and fc.origem='PD'),'') 
when '' 
then (case isnull((select top 1 fopd.fopdstamp 
from fopd 
inner join fc on fc.fcstamp=fopd.fcstamp 
where fc.fcstamp=deleted.fcstamp and fc.origem='PD'),'') 
when '' then 0 else 1 end)
else 1 end)
from deleted 
inner join fc on fc.fcstamp=deleted.fcstamp 
inner join pd on pd.pdstamp=fc.pdstamp 
where fc.fcstamp =deleted.fcstamp 
and deleted.fcorigem='PD'update fc
set fc.credf =fc.credf +isnull((select (case when sum(inserted.cambio)=0 and sum(inserted.escrec)>0 then sum(inserted.escrec)-sum(inserted.arred)
else case when sum(inserted.cambio)<>0 and  sum(inserted.escrec)>0 then sum(inserted.escrec) else 0 end end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1), 0)
, fc.debf =fc.debf -isnull((select (case when sum(inserted.cambio)=0 and sum(inserted.escrec)<0 then sum(inserted.escrec)- sum(inserted.arred)
else case when sum(inserted.cambio)<>0 and sum(inserted.escrec)<0 then sum(inserted.escrec) else 0 end end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.credfm =fc.credfm+ isnull((select (case when sum(inserted.ecambio)<>0 and sum(inserted.rec)>0 then sum(inserted.rec) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.debfm =fc.debfm -isnull((select (case when sum(inserted.ecambio)<>0 and sum(inserted.rec)<0 then sum(inserted.rec) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.ecredf =fc.ecredf +isnull((select (case when sum(inserted.erec)>0 then sum(inserted.erec)-sum(inserted.earred) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.edebf =fc.edebf -isnull((select (case when sum(inserted.erec)<0 then sum(inserted.erec)- sum(inserted.earred) else 0 end)
from inserted
where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.virsreg =fc.virsreg + isnull((select sum(inserted.virs) from inserted where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
, fc.evirsreg =fc.evirsreg+ isnull((select sum(inserted.evirs) from inserted where inserted.fcstamp=fc.fcstamp and inserted.process=1),0)
from inserted
where inserted.fcstamp =fc.fcstamp and inserted.process=1

update fc
set fc.evalpo =fc.evalpo +(select sum(inserted.erec -inserted.eivacativado) from inserted where inserted.fcstamp =fc.fcstamp)
, fc.valpo =fc.valpo +(select sum(inserted.escrec -inserted.ivacativado) from inserted where inserted.fcstamp =fc.fcstamp)
, fc.mvalpo =fc.mvalpo +(select sum(inserted.rec  -inserted.mivacativado) from inserted where inserted.fcstamp =fc.fcstamp)
, fc.ultdoc ='Doc. nº '+convert(char(10),inserted.rno)+' de '+convert(char(10),inserted.rdata,104)
, fc.recibado =1
from inserted
where fc.fcstamp =inserted.fcstamp

update FC
set	fc.irsdif =fc.irsdif +(select sum(case when inserted.desconto=100 or po.fin=100 then 0 else (ROUND((inserted.virs/ (1-(case when inserted.desconto<>0
then inserted.desconto else po.fin end/100))),0)-inserted.virs) end)
from inserted
inner join po on po.postamp=inserted.postamp where inserted.fcstamp=fc.fcstamp)
, fc.eirsdif =fc.eirsdif +(select sum(case when inserted.desconto=100 or po.fin=100 then 0 else (ROUND((inserted.evirs/(1-(case when inserted.desconto<>0
then inserted.desconto else po.fin end/100))),2)-inserted.evirs) end)
from inserted
inner join po on po.postamp=inserted.postamp where inserted.fcstamp=fc.fcstamp)
from inserted
inner join po on po.postamp =inserted.postamp
where fc.fcstamp =inserted.fcstamp
and inserted.process=1 and (inserted.desconto<>0 or po.fin<>0)

if ( select count(*) from inserted where inserted.fcorigem='PD')>0 
update pd 
set pd.integrado =1
from inserted
inner join fc on fc.fcstamp =inserted.fcstamp
inner join pd on pd.pdstamp =fc.pdstamp
where fc.fcstamp =inserted.fcstamp and inserted.fcorigem='PD'GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lo_insert ON lo WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif (Select count(*) from inserted where inserted.cm=4 or inserted.cm=22 or inserted.cm=8 or inserted.cm=17)>0 DELETE from OD from inserted where od.lpstamp=inserted.lpstamp and od.origem='LP' and (inserted.cm=4 or inserted.cm=22 or inserted.cm=8 or inserted.cm=17) if (Select count(*) from inserted where inserted.cm=2 or inserted.cm=28 or inserted.cm=6)>0 if (Select count(*) from od,inserted where od.lpstamp=inserted.lpstamp and od.origem='LP')=0 INSERT INTO OD (lpstamp,lostamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local) select lp.lpstamp,inserted.lostamp,inserted.lostamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LP',lp.nomeserie,ltrim(str(inserted.letra,15,0)),lp.vdata,lp.valor-lp.regvalor,lp.evalor-lp.eregvalor,ltrim(rtrim(lp.nome))+'-'+inserted.lodesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lp.fref,lp.ccusto,lp.ncusto,inserted.ollocal,inserted.contado,inserted.localol from inserted inner join lp on lp.lpstamp=inserted.lpstamp inner join oc on oc.olcodigo='P00001' where ((lp.valor-lp.regvalor)<>0 or (lp.evalor-lp.eregvalor)<>0) and (inserted.cm=2 or inserted.cm=28 or inserted.cm=6) else UPDATE OD SET od.lostamp=inserted.lostamp,od.usrdata=inserted.usrdata,od.usrhora=inserted.usrhora,od.usrinis=inserted.usrinis,od.data=lp.vdata,od.said=(lp.valor-lp.regvalor),od.esaid=(lp.evalor-lp.eregvalor),od.descricao=ltrim(rtrim(lp.nome))+'-'+case when inserted.cm=6 then od.descricao else inserted.lodesc end,od.fref=lp.fref,od.ccusto=lp.ccusto,od.ncusto=lp.ncusto,od.ollocal=inserted.ollocal,od.contado=inserted.contado,od.local=inserted.localol from inserted inner join lp on lp.lpstamp=inserted.lpstamp where od.lpstamp=inserted.lpstamp and od.origem='LP' and (inserted.cm=2 or inserted.cm=28 or inserted.cm=6) if (Select count(*) from inserted where inserted.cm=22)>0 INSERT INTO FC (lpstamp,fcstamp,lostamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,dataven,no,estab,nome,cm,cmdesc,origem,cred,ecred,adoc,fref,intid,ccusto,ncusto,pais,obs,moeda,zona) select lp.lpstamp,inserted.lostamp,inserted.lostamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,lp.sdata,lp.fcdata,lp.no,lp.estab,lp.nome,81,(select cmdesc from cm1 (nolock) where cm=81),'LP',inserted.valor,inserted.evalor,ltrim(str(inserted.letra,10,0)),lp.fref,lp.intid,lp.ccusto,lp.ncusto,lp.pais,'Anulação do título de dívida nº '+ltrim(str(inserted.letra,10,0)),'PTE ou EURO',fl.zona from inserted inner join lp on lp.lpstamp=inserted.lpstamp inner join fl on lp.no=fl.no and lp.estab=fl.estab where inserted.cm=22 and (inserted.valor<>0 or inserted.evalor<>0) if (Select count(*) from inserted where inserted.cm=4 or inserted.cm=29 or inserted.cm=6)>0 INSERT INTO OL (lpstamp,lostamp,olstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,moeda) select lp.lpstamp,inserted.lostamp,inserted.lostamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LP',lp.nomeserie+' nº '+ltrim(str(inserted.letra,15,0)),inserted.cheque,inserted.badata,inserted.badata,case when inserted.cm=29 then inserted.despesas+inserted.juros else inserted.valor+inserted.despesas+inserted.juros end,case when inserted.cm=29 then inserted.edespesas+inserted.ejuros else inserted.evalor+inserted.edespesas+inserted.ejuros end,ltrim(rtrim(lp.nome))+'-'+inserted.lodesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lp.fref,lp.intid,lp.ccusto,lp.ncusto,inserted.ollocal,inserted.contado,inserted.localol,'PTE ou EURO' from inserted inner join lp on lp.lpstamp=inserted.lpstamp inner join oc on oc.olcodigo='P00001' where inserted.cm=4 or inserted.cm=29 or inserted.cm=6 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_lo_update ON lo WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif (Select count(*) from inserted where inserted.cm=2 or inserted.cm=28 or inserted.cm=6)>0 if (Select count(*) from od,inserted where od.lpstamp=inserted.lpstamp and od.origem='LP')=0 INSERT INTO OD (lpstamp,lostamp,odstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,ollocal,contado,local) select lp.lpstamp,inserted.lostamp,inserted.lostamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LP',lp.nomeserie,ltrim(str(inserted.letra,15,0)),lp.vdata,lp.valor-lp.regvalor,lp.evalor-lp.eregvalor,ltrim(rtrim(lp.nome))+'-'+inserted.lodesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lp.fref,lp.ccusto,lp.ncusto,inserted.ollocal,inserted.contado,inserted.localol from inserted inner join lp on lp.lpstamp=inserted.lpstamp inner join oc on oc.olcodigo='P00001' where ((lp.valor-lp.regvalor)<>0 or (lp.evalor-lp.eregvalor)<>0) and (inserted.cm=2 or inserted.cm=28 or inserted.cm=6) and inserted.currec=1 else UPDATE OD SET od.lostamp=inserted.lostamp,od.usrdata=inserted.usrdata,od.usrhora=inserted.usrhora,od.usrinis=inserted.usrinis,od.data=lp.vdata,od.said=(lp.valor-lp.regvalor),od.esaid=(lp.evalor-lp.eregvalor),od.descricao=ltrim(rtrim(lp.nome))+'-'+case when inserted.cm=6 then od.descricao else inserted.lodesc end,od.fref=lp.fref,od.ccusto=lp.ccusto,od.ncusto=lp.ncusto,od.ollocal=inserted.ollocal,od.contado=inserted.contado,od.local=inserted.localol from inserted inner join lp on lp.lpstamp=inserted.lpstamp where od.lpstamp=inserted.lpstamp and od.origem='LP' and (inserted.cm=2 or inserted.cm=28 or inserted.cm=6) and inserted.currec=1 if (Select count(*) from inserted where inserted.cm=22)>0 if (Select count(*) from fc,inserted where fc.lostamp=inserted.lostamp and inserted.cm=22)=0 INSERT INTO FC (lpstamp,fcstamp,lostamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,dataven,no,estab,nome,cm,cmdesc,origem,cred,ecred,adoc,fref,intid,ccusto,ncusto,pais,obs,zona) select lp.lpstamp,inserted.lostamp,inserted.lostamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,lp.sdata,lp.fcdata,lp.no,lp.estab,lp.nome,81,(select cmdesc from cm1 (nolock) where cm=81),'LP',inserted.valor,inserted.evalor,ltrim(str(inserted.letra,10,0)),lp.fref,lp.intid,lp.ccusto,lp.ncusto,lp.pais,'Anulação do título de dívida nº '+ltrim(str(inserted.letra,10,0)),fl.zona from inserted inner join lp on lp.lpstamp=inserted.lpstamp inner join fl on lp.no=fl.no and lp.estab=fl.estab where inserted.currec=1 and inserted.cm=22 and (inserted.valor<>0 or inserted.evalor<>0) else UPDATE FC SET fc.usrdata=inserted.usrdata,fc.usrhora=inserted.usrhora,fc.usrinis=inserted.usrinis,fc.datalc=lp.sdata,fc.dataven=lp.fcdata,fc.fref=lp.fref,fc.intid=lp.intid,fc.ccusto=lp.ccusto,fc.ncusto=lp.ncusto from inserted inner join lp on lp.lpstamp=inserted.lpstamp where fc.lostamp=inserted.lostamp and inserted.cm=22 and inserted.currec=1 if (Select count(*) from inserted where inserted.cm=4 or inserted.cm=29 or inserted.cm=6)>0 if (Select count(*) from ol,inserted where ol.lostamp=inserted.lostamp and (inserted.cm=4 or inserted.cm=29 or inserted.cm=6))=0 INSERT INTO OL (lpstamp,lostamp,olstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,said,esaid,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,moeda) select lp.lpstamp,inserted.lostamp,inserted.lostamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'LP',lp.nomeserie+' nº '+ltrim(str(inserted.letra,15,0)),inserted.cheque,inserted.badata,inserted.badata,case when inserted.cm=29 then inserted.despesas+inserted.juros else inserted.valor+inserted.despesas+inserted.juros end,case when inserted.cm=29 then inserted.edespesas+inserted.ejuros else inserted.evalor+inserted.edespesas+inserted.ejuros end,ltrim(rtrim(lp.nome))+'-'+inserted.lodesc,oc.olcodigo,oc.olgrupo,oc.olsgrupo,lp.fref,lp.intid,lp.ccusto,lp.ncusto,inserted.ollocal,inserted.contado,inserted.localol, 'PTE ou EURO'from inserted inner join lp on lp.lpstamp=inserted.lpstamp inner join oc on oc.olcodigo='P00001' where (inserted.cm=4 or inserted.cm=29 or inserted.cm=6) and inserted.currec=1 else UPDATE OL SET ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,ol.data=inserted.badata,ol.dvalor=inserted.badata,ol.descricao=ltrim(rtrim(lp.nome))+'-'+inserted.lodesc,ol.fref=lp.fref,ol.intid=lp.intid,ol.ccusto=lp.ccusto,ol.ncusto=lp.ncusto,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.localol,ol.moeda='PTE ou EURO' from inserted inner join lp on lp.lpstamp=inserted.lpstamp where ol.lostamp=inserted.lostamp and (inserted.cm=4 or inserted.cm=29 or inserted.cm=6) and inserted.currec=1 GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rd_insert ON rd WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON				if EXISTS (SELECT 1
					FROM tsrd INNER JOIN inserted on inserted.ndoc = tsrd.ndoc
					AND tsrd.docsimport = 0 and inserted.assinatura = '')

				BEGIN
					DECLARE @ErrorMessage NVARCHAR(2000)
					DECLARE @ErrorSeverity INT
					DECLARE @ErrorState INT

					SET @ErrorSeverity = 16
					SET @ErrorState = 1

					SELECT TOP 1
							@ErrorMessage = 'A assinatura digital no documento ' + rtrim(inserted.nmdoc) + ' n.º ' + cast(inserted.rno as varchar(25)) +  ' está vazia. Por favor contacte o administrador do sistema.'
					FROM tsrd INNER JOIN inserted on inserted.ndoc = tsrd.ndoc AND tsrd.docsimport = 0 and inserted.assinatura = ''

					RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
					ROLLBACK
				ENDINSERT INTO CC (ccstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,formapag,situacao,datalc,no,nome,cm,cmdesc,origem,cobranca,cred,ecred,credm, deb,debf,edeb,edebf,debm,debfm,  ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs, cambiofixo , eivacativado, ivacativado, mivacativado) select inserted.rdstamp,inserted.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.formapag,'1',inserted.rdata,inserted.no,inserted.nome,inserted.cm,inserted.cmdesc,'RD',inserted.cobranca, case when tsrd.regrd = 0 then inserted.total else 0 end,   case when tsrd.regrd = 0 then inserted.etotal else 0 end,   case when tsrd.regrd = 0 then inserted.totalmoeda else 0 end,   case when tsrd.regrd = 1 then inserted.total*-1 else 0 end,   case when tsrd.regrd = 1 then inserted.total*-1 else 0 end,   case when tsrd.regrd = 1 then inserted.etotal*-1 else 0 end,   case when tsrd.regrd = 1 then inserted.etotal*-1 else 0 end,  case when tsrd.regrd = 1 then inserted.totalmoeda*-1 else 0 end,   case when tsrd.regrd = 1 then inserted.totalmoeda*-1 else 0 end,  case when inserted.tabiva=1 then inserted.ivav else 0 end,case when inserted.tabiva=2 then inserted.ivav else 0 end,case when inserted.tabiva=3 then inserted.ivav else 0 end,case when inserted.tabiva=4 then inserted.ivav else 0 end,case when inserted.tabiva=5 then inserted.ivav else 0 end,case when inserted.tabiva=6 then inserted.ivav else 0 end,case when inserted.tabiva=7 then inserted.ivav else 0 end,case when inserted.tabiva=8 then inserted.ivav else 0 end,case when inserted.tabiva=9 then inserted.ivav else 0 end,case when inserted.tabiva=1 then inserted.eivav else 0 end,case when inserted.tabiva=2 then inserted.eivav else 0 end,case when inserted.tabiva=3 then inserted.eivav else 0 end,case when inserted.tabiva=4 then inserted.eivav else 0 end,case when inserted.tabiva=5 then inserted.eivav else 0 end,case when inserted.tabiva=6 then inserted.eivav else 0 end,case when inserted.tabiva=7 then inserted.eivav else 0 end,case when inserted.tabiva=8 then inserted.eivav else 0 end,case when inserted.tabiva=9 then inserted.eivav else 0 end,inserted.moeda,inserted.rno,inserted.rdata,inserted.fref,inserted.intid,inserted.cobrador,inserted.rota,inserted.ccusto,inserted.ncusto,inserted.zona,inserted.vendedor,inserted.vendnm,inserted.segmento,inserted.tipo,inserted.pais,inserted.estab,  inserted.descricao, inserted.cambiofixo , isnull(-inserted.eivacativado, 0), isnull(-inserted.ivacativado, 0),											isnull(-inserted.mivacativado, 0) from inserted inner join tsrd on tsrd.ndoc = inserted.ndoc   if (select isnull(count(*),0) from inserted inner join tsrd on tsrd.ndoc = inserted.ndoc where tsrd.regrd = 1) > 0  BEGIN  update cc  set cc.credf = cc.credf+ inserted.total*-1  , cc.ecredf = cc.ecredf + inserted.etotal*-1  , cc.credfm = cc.credfm+ inserted.totalmoeda*-1  from inserted,cc	where inserted.ordstamp = cc.rdstamp  END INSERT INTO OL (olstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque,data,dvalor,entr,eentr,entrm,said,esaid,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext,cecope) select inserted.rdstamp,inserted.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'RD',(case when (select oldoc from tsrd where ndoc=inserted.ndoc)=' ' then inserted.cmdesc else (select oldoc from tsrd where ndoc=inserted.ndoc) end),convert(char(15),inserted.rno),inserted.rdata,inserted.rdata,  case when tsrd.regrd = 0 then inserted.total - inserted.ivacativado else 0 end,  case when tsrd.regrd = 0 then inserted.etotal - inserted.eivacativado else 0 end,  case when tsrd.regrd = 0 then inserted.valorm2 - inserted.mivacativado else 0 end,  case when tsrd.regrd = 1 then inserted.total*-1 + inserted.ivacativado else 0 end,  case when tsrd.regrd = 1 then inserted.etotal*-1 + inserted.ivacativado else 0 end,  case when tsrd.regrd = 1 then inserted.valorm2*-1 + inserted.ivacativado else 0 end, inserted.moeda2,inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.telocal,inserted.site,inserted.pnome,inserted.pno,inserted.cxstamp,inserted.cxusername,inserted.ssstamp,inserted.ssusername,inserted.operext,inserted.cecope from inserted inner join oc on oc.olcodigo=inserted.olcodigo inner join tsrd on tsrd.ndoc = inserted.ndoc  if (select isnull(count(*),0) from inserted inner join tsrd on tsrd.ndoc = inserted.ndoc where tsrd.regrd = 1) = 0  begin INSERT INTO CH (chstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,data,dataem,datareg,no,nome,estab,origem,valor,evalor,valorm,moeda,clbanco,clcheque, fref,tptit, contado, ollocal) select inserted.rdstamp,inserted.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,inserted.chdata,inserted.ousrdata,inserted.ousrdata,inserted.no,inserted.nome,inserted.estab,'RD',inserted.total-inserted.ivacativado,inserted.etotal-inserted.eivacativado,inserted.valorm2-inserted.mivacativado,inserted.moeda2,inserted.clbanco,inserted.clcheque, inserted.fref,inserted.tptit, inserted.contado, inserted.ollocal from inserted where inserted.cheque=1  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fo_insert ON fo WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONif EXISTS(SELECT 1 FROM fo2 as tempfo2
inner join inserted ON inserted.fostamp COLLATE Latin1_General_CI_AI = tempfo2.fo2stamp COLLATE Latin1_General_CI_AI and tempfo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
inner join cm1 ON inserted.doccode = cm1.cm and cm1.tiposaft COLLATE Latin1_General_CI_AI<>'' COLLATE Latin1_General_CI_AI)
begin
declare @ErrorMessage NVARCHAR(2000)
declare @ErrorSeverity INT
declare @ErrorState INT
set @ErrorSeverity = 16
set @ErrorState = 1
SELECT TOP 1
@ErrorMessage = 'A assinatura digital no documento ' + rtrim(inserted.docnome) + ' n.º ' + RTRIM(inserted.adoc) +  ' está vazia. Por favor contacte o administrador do sistema.'
FROM fo2 as tempfo2
inner join inserted ON inserted.fostamp COLLATE Latin1_General_CI_AI = tempfo2.fo2stamp COLLATE Latin1_General_CI_AI
and tempfo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
inner join cm1 ON inserted.doccode = cm1.cm and cm1.tiposaft COLLATE Latin1_General_CI_AI<>'' COLLATE Latin1_General_CI_AI
RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
ROLLBACK
endif (select 1 from fo2 inner join inserted on fo2.fo2stamp=inserted.fostamp inner join cm1 on inserted.doccode=cm1.cm where cm1.autofat=1)>0
begin
update fo2
set fo2.ROWIDINDEX =(case when cm1.autofat =1  then
REPLACE(CAST(inserted.doccode AS varchar(8)),' ','') +REPLACE(CAST(inserted.adoc as varchar(50)),' ','')+
REPLACE(CAST(inserted.foano as varchar(4)),' ','')  + '1'
else cast(NEWID() as varchar(36)) end )
from inserted inner join fo2 on fo2.fo2stamp=inserted.fostamp
inner join cm1 on inserted.doccode = cm1.cm

end


update FL
set fl.acmfact =fl.acmfact+inserted.ttiliq
, fl.eacmfact=fl.eacmfact+inserted.ettiliq
from INSERTED
inner join fo2 on inserted.fostamp =fo2.fo2stamp and fo2.anulado =0
where fl.no =inserted.no

insert into FC
( tpdesc, tpstamp, fcstamp, fostamp, usrdata, usrhora, usrinis, ousrdata, ousrhora, ousrinis
, datalc, no, nome, cm, cmdesc, origem
, cred, ecred, deb, edeb, credm, debm
, aprovado, nmaprov, dtaprov, ivav1, ivav2, ivav3, ivav4, ivav5, ivav6, ivav7, ivav8, ivav9
, eivav1, eivav2, eivav3, eivav4, eivav5, eivav6, eivav7, eivav8, eivav9
, ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9
, reexgiva, datasup12, formapag
, eivacativado, ivacativado, mivacativado
, moeda, adoc, dataven, fref, cambiofixo, ccusto, ncusto, intid, tipo, estab, pais, zona, virs, evirs, crend, obs
, tiporend
)
select inserted.tpdesc, inserted.tpstamp, inserted.fostamp, inserted.fostamp
, inserted.usrdata, inserted.usrhora, inserted.usrinis
, inserted.ousrdata, inserted.ousrhora, inserted.ousrinis
, inserted.data, inserted.no, inserted.nome, inserted.doccode, inserted.docnome,'FO'
, case when cm1.debito =1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj <> 0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end
, case when cm1.debito =1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj <> 0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end
, case when cm1.debito =1 then ( case when inserted.pais=2 or fo2.ttinvsuj <> 0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end
, case when cm1.debito =1 then ( case when inserted.pais=2 or fo2.ettinvsuj <> 0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end
, case when cm1.debito =1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end
, case when cm1.debito =1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end
, inserted.aprovado, inserted.nmaprov, inserted.dtaprov
, inserted.ivav1 - fo2.ivainvsuj1, inserted.ivav2 - fo2.ivainvsuj2, inserted.ivav3 - fo2.ivainvsuj3
, inserted.ivav4 - fo2.ivainvsuj4, inserted.ivav5 - fo2.ivainvsuj5, inserted.ivav6 - fo2.ivainvsuj6
, inserted.ivav7 - fo2.ivainvsuj7, inserted.ivav8 - fo2.ivainvsuj8, inserted.ivav9 - fo2.ivainvsuj9
, inserted.eivav1 - fo2.eivainvsuj1, inserted.eivav2 - fo2.eivainvsuj2, inserted.eivav3 - fo2.eivainvsuj3
, inserted.eivav4 - fo2.eivainvsuj4, inserted.eivav5 - fo2.eivainvsuj5, inserted.eivav6 - fo2.eivainvsuj6
, inserted.eivav7 - fo2.eivainvsuj7, inserted.eivav8 - fo2.eivainvsuj8, inserted.eivav9 - fo2.eivainvsuj9
, isnull(fo2.ivatx1,0), isnull(fo2.ivatx2,0),isnull(fo2.ivatx3,0), isnull(fo2.ivatx4,0), isnull(fo2.ivatx5,0)
, isnull(fo2.ivatx6,0), isnull(fo2.ivatx7,0), isnull(fo2.ivatx8,0), isnull(fo2.ivatx9,0)
, isnull(fo2.reexgiva,0) , dateadd(Month,13, inserted.docdata-DAY(inserted.docdata)) , isnull(fo2.formapag,'1')

, isnull(fo2.eivacativado, 0), isnull(fo2.ivacativado, 0), isnull(fo2.mivacativado, 0)
, inserted.moeda, inserted.adoc, inserted.pdata, inserted.fref, inserted.cambiofixo, inserted.ccusto, inserted.ncusto, inserted.intid, inserted.tipo, inserted.estab, inserted.pais, inserted.zona
, case when cm1.debito=1 then inserted.virs*-1 else inserted.virs end,case when cm1.debito=1 then inserted.evirs*-1 else inserted.evirs end
, inserted.crend, inserted.final, fo2.tiporend
from inserted
inner join cm1 on cm1.cm= inserted.doccode
inner join fo2 on fo2.fo2stamp =inserted.fostamp
where cm1.folanfc =1 and inserted.multi=0

update FC set
debf =(case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end )
, edebf =(case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end )
, debfm =(case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end)
, credf =(case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end)
, ecredf =(case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end)
, credfm =(case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end)
, virsreg =(case when cm1.debito=1 then inserted.virs*-1 else inserted.virs end)
, evirsreg =(case when cm1.debito=1 then inserted.evirs*-1 else inserted.evirs end)
, eivacatreg = isnull(fo2.eivacativado, 0)
, ivacatreg = isnull(fo2.ivacativado, 0)
, mivacatreg = isnull(fo2.mivacativado, 0)
, obs =inserted.final
, tiporend = fo2.tiporend
from inserted
inner join cm1 on cm1.cm =inserted.doccode
inner join fo2 on fo2.fo2stamp =inserted.fostamp
inner join fc on fc.fostamp=inserted.fostamp
where cm1.folanfc =1 and cm1.fcautoreg =1

insert into FC
(tpdesc, tpstamp, fcstamp, fostamp, usrdata, usrhora, usrinis, ousrdata, ousrhora, ousrinis, datalc, no, nome, cm, cmdesc, origem
, cred, ecred, deb, edeb, credm,debm, credf, ecredf, debf, edebf, credfm, debfm
, aprovado, nmaprov, dtaprov
, ivav1, ivav2, ivav3, ivav4, ivav5, ivav6, ivav7, ivav8, ivav9
, eivav1, eivav2, eivav3, eivav4, eivav5, eivav6, eivav7, eivav8, eivav9
, ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9
, eivacativado, ivacativado, mivacativado
, eivacatreg, ivacatreg, mivacatreg
, reexgiva, datasup12, formapag
, moeda,adoc,dataven,fref,cambiofixo,ccusto,ncusto,intid,tipo,estab,pais,zona,crend,obs,tiporend)
select inserted.tpdesc, inserted.tpstamp
, substring('R'+inserted.fostamp,1,25), inserted.fostamp
, inserted.usrdata, inserted.usrhora, inserted.usrinis
, inserted.ousrdata, inserted.ousrhora, inserted.ousrinis
, inserted.data, inserted.no, inserted.nome,cm1.cmfcautoreg,cm1.cmfcautoregn, 'FO'
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end
, inserted.aprovado, inserted.nmaprov, inserted.dtaprov
, inserted.ivav1 - fo2.ivainvsuj1, inserted.ivav2 - fo2.ivainvsuj2, inserted.ivav3 - fo2.ivainvsuj3
, inserted.ivav4 - fo2.ivainvsuj4, inserted.ivav5 - fo2.ivainvsuj5, inserted.ivav6 - fo2.ivainvsuj6
, inserted.ivav7 - fo2.ivainvsuj7, inserted.ivav8 - fo2.ivainvsuj8, inserted.ivav9 - fo2.ivainvsuj9
, inserted.eivav1 - fo2.eivainvsuj1, inserted.eivav2 - fo2.eivainvsuj2, inserted.eivav3 - fo2.eivainvsuj3
, inserted.eivav4 - fo2.eivainvsuj4, inserted.eivav5 - fo2.eivainvsuj5, inserted.eivav6 - fo2.eivainvsuj6
, inserted.eivav7 - fo2.eivainvsuj7, inserted.eivav8 - fo2.eivainvsuj8, inserted.eivav9 - fo2.eivainvsuj9
, isnull(fo2.ivatx1,0),isnull(fo2.ivatx2,0),isnull(fo2.ivatx3,0)
, isnull(fo2.ivatx4,0),isnull(fo2.ivatx5,0),isnull(fo2.ivatx6,0)
, isnull(fo2.ivatx7,0),isnull(fo2.ivatx8,0),isnull(fo2.ivatx9,0)
, isnull(fo2.eivacativado, 0)
, isnull(fo2.ivacativado, 0)
, isnull(fo2.mivacativado, 0)
, isnull(fo2.eivacativado, 0)
, isnull(fo2.ivacativado, 0)
, isnull(fo2.mivacativado, 0)
, isnull(fo2.reexgiva,0),
dateadd(Month,13, inserted.docdata-DAY(inserted.docdata)),
isnull(fo2.formapag,'1'),
 inserted.moeda, inserted.adoc, inserted.pdata, inserted.fref, inserted.cambiofixo, inserted.ccusto, inserted.ncusto, inserted.intid, inserted.tipo,
 inserted.estab, inserted.pais, inserted.zona, inserted.crend, inserted.final, fo2.tiporend
 from inserted inner join cm1 on cm1.cm =inserted.doccode inner join fo2 on fo2.fo2stamp =inserted.fostamp where cm1.folanfc=1 and cm1.fcautoreg=1 and inserted.multi=0

insert into OL
(olstamp,fostamp, usrdata,usrhora,usrinis, ousrdata,ousrhora,ousrinis,origem,documento,cheque, data
,dvalor,said,esaid,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,intid,ollocal,contado
,local,site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext)
select inserted.fostamp, inserted.fostamp, inserted.usrdata, inserted.usrhora, inserted.usrinis
, inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,'FO', inserted.docnome, inserted.adoc, inserted.data, inserted.data
,( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-inserted.rdtotal-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) -inserted.virs - fo2.ivacativado else inserted.total-inserted.rdtotal-inserted.virs - fo2.ivacativado end )
,( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.etotal-inserted.erdtotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end)-inserted.evirs - fo2.eivacativado else inserted.etotal-inserted.erdtotal-inserted.evirs - fo2.eivacativado end )
, inserted.valorm2, inserted.moeda2,
inserted.nome, (case when fo2.olcodigo<>'' then fo2.olcodigo else cm1.olfl end) ,
(case when fo2.olcodigo<>'' then (select oc.olgrupo from oc where oc.olcodigo = fo2.olcodigo) else (select oc.olgrupo from oc where oc.olcodigo = cm1.olfl) end),
(case when fo2.olcodigo<>'' then (select oc.olsgrupo from oc where oc.olcodigo = fo2.olcodigo) else (select oc.olsgrupo from oc where oc.olcodigo = cm1.olfl) end),
inserted.fref, inserted.ccusto, inserted.ncusto, inserted.intid, inserted.ollocal, inserted.contado, inserted.telocal,
inserted.site, inserted.pnome, inserted.pno, inserted.cxstamp, inserted.cxusername,
inserted.ssstamp, inserted.ssusername,isnull(fo2.operext, 0)
 from inserted inner join fo2 on fo2.fo2stamp =inserted.fostamp and fo2.anulado =0 inner join cm1 on cm1.cm =inserted.doccode inner join oc on oc.olcodigo =(case when cm1.olfl<>'' then cm1.olfl else fo2.olcodigo end) where cm1.folanol=1

if (select count(*)
from inserted
inner join fc on fc.fostamp = inserted.fostamp
inner join fo2 on fo2.fo2stamp=inserted.fostamp
where fo2.anulado=1
)>0
begin
update FC set
deb = 0, edeb = 0, debf = 0, edebf = 0,debfm = 0,
cred = 0, ecred = 0, credf = 0, ecredf = 0, credfm = 0,
ivav1 = 0, ivav2 = 0, ivav3 = 0, ivav4 = 0, ivav5 = 0, ivav6 = 0, ivav7 = 0, ivav8 = 0, ivav9 = 0,
eivav1 = 0, eivav2 = 0, eivav3 = 0, eivav4 = 0, eivav5 = 0, eivav6 = 0, eivav7 = 0, eivav8 = 0, eivav9 = 0,
ivatx1 = 0, ivatx2 = 0, ivatx3 = 0, ivatx4 = 0, ivatx5 = 0, ivatx6 = 0, ivatx7 = 0, ivatx8 = 0, ivatx9 = 0,
reexgiva = 0, virs = 0, evirs = 0,
virsreg = 0, evirsreg = 0,
difcambio = 0, edifcambio = 0
from inserted
inner join fo2 on fo2.fo2stamp=inserted.fostamp
inner join fc on fc.fostamp = inserted.fostamp
where fo2.anulado = 1
end


if exists (select 1
from inserted
inner join fo2 on fo2.fo2stamp=inserted.fostamp
where fo2.cheque <> '')
begin
update chf
set deststamp = fo2.fo2stamp
, destino = 'FO'
, nome = case when chf.nome = '' then inserted.nome else chf.nome end
, no = case when chf.no = 0 then inserted.no else chf.no end
, estab = case when chf.no = 0 then inserted.estab else chf.estab end
from inserted
inner join fo2 on fo2.fo2stamp = inserted.fostamp
inner join chf on fo2.chfstamp = chf.chfstamp
where fo2.cheque <> ''
end
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fo_update ON fo WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfo VARBINARY(128)
SELECT @Cinfo = Context_Info()
IF @Cinfo = 0x22222
RETURN
if EXISTS(SELECT 1 FROM fo2 as tempfo2
inner join inserted ON inserted.fostamp COLLATE Latin1_General_CI_AI = tempfo2.fo2stamp COLLATE Latin1_General_CI_AI and tempfo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
inner join cm1 ON inserted.doccode = cm1.cm and cm1.tiposaft COLLATE Latin1_General_CI_AI<>'' COLLATE Latin1_General_CI_AI)
begin
declare @ErrorMessage NVARCHAR(2000)
declare @ErrorSeverity INT
declare @ErrorState INT
set @ErrorSeverity = 16
set @ErrorState = 1
SELECT TOP 1
@ErrorMessage = 'A assinatura digital no documento ' + rtrim(inserted.docnome) + ' n.º ' + RTRIM(inserted.adoc) +  ' está vazia. Por favor contacte o administrador do sistema.'
FROM fo2 as tempfo2
inner join inserted ON inserted.fostamp COLLATE Latin1_General_CI_AI = tempfo2.fo2stamp COLLATE Latin1_General_CI_AI
and tempfo2.assinatura COLLATE Latin1_General_CI_AI = '' COLLATE Latin1_General_CI_AI
inner join cm1 ON inserted.doccode = cm1.cm and cm1.tiposaft COLLATE Latin1_General_CI_AI<>'' COLLATE Latin1_General_CI_AI
RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)
ROLLBACK
end


if (update(doccode) or  update(adoc) or update(foano))
Begin
update fo2
set fo2.ROWIDINDEX =(case when cm1.autofat =1  then
REPLACE(CAST(inserted.doccode AS varchar(8)),' ','') +REPLACE(CAST(inserted.adoc as varchar(50)),' ','')+
REPLACE(CAST(inserted.foano as varchar(4)),' ','')  + '1'
else cast(NEWID() as varchar(36)) end )
from inserted inner join fo2 on fo2.fo2stamp=inserted.fostamp
inner join cm1 on inserted.doccode = cm1.cm

End
IF (UPDATE(ttiliq) OR UPDATE(ettiliq) OR UPDATE(no) OR UPDATE(tpdesc) OR UPDATE(data) OR UPDATE(nome)
OR UPDATE(doccode) OR UPDATE(docnome) OR UPDATE(moeda) OR UPDATE(adoc) OR UPDATE(pdata) OR UPDATE(fref) OR UPDATE(cambiofixo) OR UPDATE(ccusto)
OR UPDATE(ncusto) OR UPDATE(intid) OR UPDATE(tipo) OR UPDATE(estab) OR UPDATE(pais) OR UPDATE(zona) OR UPDATE(aprovado) OR UPDATE(nmaprov) OR UPDATE(dtaprov)
OR UPDATE(ivav1) OR UPDATE(ivav2) OR UPDATE(ivav3) OR UPDATE(ivav4) OR UPDATE(ivav5) OR UPDATE(ivav6) OR UPDATE(ivav7) OR UPDATE(ivav8) OR UPDATE(ivav9)
OR UPDATE(eivav1) OR UPDATE(eivav2) OR UPDATE(eivav3) OR UPDATE(eivav4) OR UPDATE(eivav5) OR UPDATE(eivav6) OR UPDATE(eivav7) OR UPDATE(eivav8) OR UPDATE(eivav9)
OR UPDATE(virs) OR UPDATE(evirs) OR UPDATE(total) OR UPDATE(ttiva) OR UPDATE(etotal) OR UPDATE(ettiva) OR UPDATE(totmoeda) OR UPDATE(tmiva)
OR UPDATE(crend) OR UPDATE(final) OR UPDATE(multi))
BEGIN

UPDATE FL set fl.acmfact=fl.acmfact-deleted.ttiliq,fl.eacmfact=fl.eacmfact-deleted.ettiliq from DELETED where fl.no=deleted.no

UPDATE FL set fl.acmfact=fl.acmfact+inserted.ttiliq,fl.eacmfact=fl.eacmfact+inserted.ettiliq from inserted where fl.no=inserted.no

delete from FC from deleted
where fc.fostamp = deleted.fostamp and deleted.fostamp not in (select inserted.fostamp from inserted)

delete from fofc from inserted inner join cm1 on inserted.doccode=cm1.cm
where (inserted.multi=0 or cm1.folanfc=0) and fofc.fostamp=inserted.fostamp

delete from FC from inserted inner join cm1 on inserted.doccode=cm1.cm
where fc.fostamp = inserted.fostamp and cm1.folanfc=0

delete from FC from deleted
where fc.fostamp = deleted.fostamp and deleted.multi=0 and deleted.fostamp not in (select inserted.fostamp from inserted where inserted.multi=0)

delete from FC from deleted
 where fc.fcstamp = substring('R'+deleted.fostamp,1,25) and deleted.multi=0

END

IF (UPDATE(tpdesc) OR UPDATE(data) OR UPDATE(no) OR UPDATE(nome)
OR UPDATE(doccode) OR UPDATE(docnome) OR UPDATE(moeda) OR UPDATE(adoc) OR UPDATE(pdata) OR UPDATE(fref) OR UPDATE(cambiofixo) OR UPDATE(ccusto)
OR UPDATE(ncusto) OR UPDATE(intid) OR UPDATE(tipo) OR UPDATE(estab) OR UPDATE(pais) OR UPDATE(zona) OR UPDATE(aprovado) OR UPDATE(nmaprov) OR UPDATE(dtaprov)
OR UPDATE(ivav1) OR UPDATE(ivav2) OR UPDATE(ivav3) OR UPDATE(ivav4) OR UPDATE(ivav5) OR UPDATE(ivav6) OR UPDATE(ivav7) OR UPDATE(ivav8) OR UPDATE(ivav9)
OR UPDATE(eivav1) OR UPDATE(eivav2) OR UPDATE(eivav3) OR UPDATE(eivav4) OR UPDATE(eivav5) OR UPDATE(eivav6) OR UPDATE(eivav7) OR UPDATE(eivav8) OR UPDATE(eivav9)
OR UPDATE(virs) OR UPDATE(evirs) OR UPDATE(total) OR UPDATE(ttiva) OR UPDATE(etotal) OR UPDATE(ettiva) OR UPDATE(totmoeda) OR UPDATE(tmiva)
OR UPDATE(crend) OR UPDATE(final) OR UPDATE(multi))
BEGIN

UPDATE FC
set fc.usrdata=inserted.usrdata, fc.usrhora=inserted.usrhora, fc.usrinis=inserted.usrinis,
fc.tpdesc=inserted.tpdesc,fc.tpstamp=inserted.tpstamp,
fc.datalc=inserted.data,fc.no=inserted.no,fc.nome=inserted.nome,fc.cm=inserted.doccode,fc.cmdesc=inserted.docnome,
fc.moeda=inserted.moeda,fc.adoc=inserted.adoc,fc.dataven=inserted.pdata,fc.fref=inserted.fref,fc.cambiofixo=inserted.cambiofixo,
fc.ccusto=inserted.ccusto,fc.ncusto=inserted.ncusto,fc.intid=inserted.intid,fc.tipo=inserted.tipo,fc.estab=inserted.estab,fc.pais=inserted.pais,fc.zona=inserted.zona,
fc.aprovado=inserted.aprovado,fc.nmaprov=inserted.nmaprov,fc.dtaprov=inserted.dtaprov,
fc.ivav1 = inserted.ivav1 - fo2.ivainvsuj1, fc.ivav2 = inserted.ivav2 - fo2.ivainvsuj2, fc.ivav3 = inserted.ivav3 - fo2.ivainvsuj3,
fc.ivav4 = inserted.ivav4 - fo2.ivainvsuj4, fc.ivav5 = inserted.ivav5 - fo2.ivainvsuj5, fc.ivav6 = inserted.ivav6 - fo2.ivainvsuj6,
fc.ivav7 = inserted.ivav7 - fo2.ivainvsuj7, fc.ivav8 = inserted.ivav8 - fo2.ivainvsuj8, fc.ivav9 = inserted.ivav9 - fo2.ivainvsuj9,
fc.eivav1 = inserted.eivav1 - fo2.eivainvsuj1, fc.eivav2 = inserted.eivav2 - fo2.eivainvsuj2, fc.eivav3 = inserted.eivav3 - fo2.eivainvsuj3,
fc.eivav4 = inserted.eivav4 - fo2.eivainvsuj4, fc.eivav5 = inserted.eivav5 - fo2.eivainvsuj5, fc.eivav6 = inserted.eivav6 - fo2.eivainvsuj6,
fc.eivav7 = inserted.eivav7 - fo2.eivainvsuj7, fc.eivav8 = inserted.eivav8 - fo2.eivainvsuj8, fc.eivav9 = inserted.eivav9 - fo2.eivainvsuj9,
 fc.ivatx1=isnull(fo2.ivatx1,0),fc.ivatx2=isnull(fo2.ivatx2,0)
,fc.ivatx3=isnull(fo2.ivatx3,0),fc.ivatx4=isnull(fo2.ivatx4,0)
,fc.ivatx5=isnull(fo2.ivatx5,0),fc.ivatx6=isnull(fo2.ivatx6,0)
,fc.ivatx7=isnull(fo2.ivatx7,0),fc.ivatx8=isnull(fo2.ivatx8,0)
,fc.ivatx9=isnull(fo2.ivatx9,0),
 fc.reexgiva=isnull(fo2.reexgiva,0), fc.tiporend = fo2.tiporend,
fc.datasup12=dateadd(Month,13, inserted.docdata-DAY(inserted.docdata)),
 fc.formapag=isnull(fo2.formapag,'1'),
fc.virs=case when cm1.debito=1 then inserted.virs*-1 else inserted.virs end,fc.evirs=case when cm1.debito=1 then inserted.evirs*-1 else inserted.evirs end,
fc.cred=case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end,
fc.ecred=case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end,
fc.deb=case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end ,
fc.edeb=case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end,
fc.credm=case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end,
fc.debm=case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end,
fc.crend=inserted.crend,
fc.obs=inserted.final
from inserted inner join cm1 on inserted.doccode=cm1.cm inner join fo2 on inserted.fostamp=fo2.fo2stamp where inserted.fostamp=fc.fostamp and inserted.multi=0 and cm1.folanfc=1
 and inserted.fostamp IN (select deleted.fostamp from deleted where deleted.multi=0 and fc.fcstamp <> substring('E'+inserted.fostamp,1,25))

insert into FC (tpdesc,tpstamp,fcstamp,fostamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
datalc,no,nome,cm,cmdesc,origem,cred,ecred,deb,edeb,
credm,debm,
aprovado,nmaprov,dtaprov,
ivav1, ivav2, ivav3, ivav4, ivav5, ivav6, ivav7, ivav8, ivav9,
eivav1, eivav2, eivav3, eivav4, eivav5, eivav6, eivav7, eivav8, eivav9,
ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9,
reexgiva, tiporend,
datasup12,
formapag,
moeda,adoc,dataven,fref,cambiofixo,ccusto,ncusto,intid,tipo,estab,pais,zona,virs,evirs,crend,obs)
select inserted.tpdesc, inserted.tpstamp, inserted.fostamp, inserted.fostamp,
inserted.usrdata, inserted.usrhora, inserted.usrinis,
inserted.ousrdata, inserted.ousrhora, inserted.ousrinis,
inserted.data, inserted.no, inserted.nome, inserted.doccode, inserted.docnome,'FO',
 case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end ,
 case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end,
 case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end,
 case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end,
 case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end,
 case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end ,
inserted.aprovado, inserted.nmaprov, inserted.dtaprov,
inserted.ivav1 - fo2.ivainvsuj1, inserted.ivav2 - fo2.ivainvsuj2, inserted.ivav3 - fo2.ivainvsuj3,
inserted.ivav4 - fo2.ivainvsuj4, inserted.ivav5 - fo2.ivainvsuj5, inserted.ivav6 - fo2.ivainvsuj6,
inserted.ivav7 - fo2.ivainvsuj7, inserted.ivav8 - fo2.ivainvsuj8, inserted.ivav9 - fo2.ivainvsuj9,
inserted.eivav1 - fo2.eivainvsuj1, inserted.eivav2 - fo2.eivainvsuj2, inserted.eivav3 - fo2.eivainvsuj3,
inserted.eivav4 - fo2.eivainvsuj4, inserted.eivav5 - fo2.eivainvsuj5, inserted.eivav6 - fo2.eivainvsuj6,
inserted.eivav7 - fo2.eivainvsuj7, inserted.eivav8 - fo2.eivainvsuj8, inserted.eivav9 - fo2.eivainvsuj9,
isnull(fo2.ivatx1,0),isnull(fo2.ivatx2,0),isnull(fo2.ivatx3,0)
,isnull(fo2.ivatx4,0),isnull(fo2.ivatx5,0)
,isnull(fo2.ivatx6,0),isnull(fo2.ivatx7,0)
,isnull(fo2.ivatx8,0),isnull(fo2.ivatx9,0)
,isnull(fo2.reexgiva,0), fo2.tiporend,
dateadd(Month,13, inserted.docdata-DAY(inserted.docdata)),
isnull(fo2.formapag,'1'),
inserted.moeda, inserted.adoc, inserted.pdata, inserted.fref, inserted.cambiofixo, inserted.ccusto, inserted.ncusto, inserted.intid, inserted.tipo, inserted.estab, inserted.pais, inserted.zona,
case when cm1.debito=1 then inserted.virs*-1 else inserted.virs end,case when cm1.debito=1 then inserted.evirs*-1 else inserted.evirs end,
inserted.crend, inserted.final
from inserted inner join cm1 on inserted.doccode=cm1.cm inner join fo2 on fo2.fo2stamp=inserted.fostamp where inserted.multi=0 and cm1.folanfc=1 and not EXISTS(select fostamp from fc where fc.fcstamp=inserted.fostamp)
END

IF (select count(*) from inserted inner join fo2 on inserted.fostamp=fo2.fo2stamp)>0
begin
delete from OL
from inserted
inner join cm1 on inserted.doccode=cm1.cm
inner join fo2 on inserted.fostamp=fo2.fo2stamp
where ol.fostamp = inserted.fostamp and (cm1.folanol=0 or fo2.anulado=1)

delete from sl
from inserted
inner join fo2 on inserted.fostamp=fo2.fo2stamp
inner join fn on fn.fostamp=inserted.fostamp
inner join sl on fn.fnstamp=sl.fnstamp
where fo2.anulado=1

delete from sv
From inserted
inner join fo2 On inserted.fostamp=fo2.fo2stamp
inner join fn On fn.fostamp=inserted.fostamp
inner join sv On fn.fnstamp=sv.fnstamp
Where fo2.anulado=1
end
IF ( UPDATE(docnome) OR UPDATE(adoc) OR UPDATE(data) OR UPDATE(pais) OR UPDATE(total) OR UPDATE(rdtotal) OR UPDATE(ttiva) OR UPDATE(virs) OR UPDATE(valorm2)
OR UPDATE(moeda2) OR UPDATE(nome) OR UPDATE(fref) OR UPDATE(ccusto) OR UPDATE(ncusto) OR UPDATE(site) OR UPDATE(pnome) OR UPDATE(pno) OR UPDATE(intid)
OR UPDATE(cxstamp) OR UPDATE(ssstamp) OR UPDATE(ssusername) OR UPDATE(doccode) OR UPDATE(ollocal) OR UPDATE(contado) OR UPDATE(telocal)
OR UPDATE(etotal) OR UPDATE(ettiva) OR UPDATE(evirs) OR UPDATE(erdtotal) )
BEGIN

UPDATE OL set
ol.olstamp=inserted.fostamp,ol.fostamp=inserted.fostamp,
ol.usrdata=inserted.usrdata,ol.usrhora=inserted.usrhora,ol.usrinis=inserted.usrinis,
ol.ousrdata=inserted.ousrdata,ol.ousrhora=inserted.ousrhora,ol.ousrinis=inserted.ousrinis,ol.origem='FO',
ol.documento=inserted.docnome,ol.cheque=inserted.adoc,
ol.data=inserted.data,ol.dvalor=inserted.data,
ol.said=( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-inserted.rdtotal-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end)-inserted.virs
 - fo2.ivacativado else inserted.total-inserted.rdtotal-inserted.virs - fo2.ivacativado end ),
ol.esaid=( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.etotal-inserted.erdtotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end)-inserted.evirs
 - fo2.eivacativado else inserted.etotal-inserted.erdtotal-inserted.evirs - fo2.eivacativado end ),
ol.saidm=inserted.valorm2,ol.moeda=inserted.moeda2,
ol.descricao=inserted.nome,ol.olcodigo= (case when fo2.olcodigo<>'' then fo2.olcodigo else cm1.olfl end) ,
ol.grupo=(case when fo2.olcodigo<>'' then (select oc.olgrupo from oc where oc.olcodigo = fo2.olcodigo) else (select oc.olgrupo from oc where oc.olcodigo = cm1.olfl) end),
ol.sgrupo=(case when fo2.olcodigo<>'' then (select oc.olsgrupo from oc where oc.olcodigo = fo2.olcodigo) else (select oc.olsgrupo from oc where oc.olcodigo = cm1.olfl) end),
ol.fref=inserted.fref,ol.ccusto=inserted.ccusto,ol.ncusto=inserted.ncusto,ol.intid=inserted.intid,ol.ollocal=inserted.ollocal,ol.contado=inserted.contado,ol.local=inserted.telocal,
ol.site=inserted.site,ol.pnome=inserted.pnome,ol.pno=inserted.pno,ol.cxstamp=inserted.cxstamp,
ol.ssstamp=inserted.ssstamp,ol.ssusername=inserted.ssusername,ol.operext=isnull(fo2.operext, 0)
from inserted inner join cm1 on cm1.cm=inserted.doccode  inner join oc on oc.olcodigo=cm1.olfl  inner join fo2 on fo2.fo2stamp=inserted.fostamp where cm1.folanol=1 and ol.fostamp=inserted.fostamp

insert into OL
(olstamp,fostamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,origem,documento,cheque
, data,dvalor,said,esaid,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,ccusto,ncusto,intid,ollocal,contado,local
, site,pnome,pno,cxstamp,cxusername,ssstamp,ssusername,operext
)
select inserted.fostamp, inserted.fostamp
, inserted.usrdata, inserted.usrhora, inserted.usrinis
, inserted.ousrdata, inserted.ousrhora, inserted.ousrinis, 'FO'
, inserted.docnome, inserted.adoc
, inserted.data, inserted.data
, ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-inserted.rdtotal-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end)-inserted.virs - fo2.ivacativado else inserted.total-inserted.rdtotal-inserted.virs - fo2.ivacativado end )
, ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.etotal-inserted.erdtotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end)-inserted.evirs - fo2.eivacativado else inserted.etotal-inserted.erdtotal-inserted.evirs - fo2.eivacativado end )
, inserted.valorm2, inserted.moeda2
, inserted.nome,(case when fo2.olcodigo<>'' then fo2.olcodigo else cm1.olfl end)
, case when fo2.olcodigo<>'' then (select oc.olgrupo from oc where oc.olcodigo = fo2.olcodigo) else (select oc.olgrupo from oc where oc.olcodigo = cm1.olfl) end as grupo
, case when fo2.olcodigo<>'' then (select oc.olsgrupo from oc where oc.olcodigo = fo2.olcodigo) else (select oc.olsgrupo from oc where oc.olcodigo = cm1.olfl) end as sgrupo
, inserted.fref, inserted.ccusto, inserted.ncusto, inserted.intid, inserted.ollocal, inserted.contado, inserted.telocal
, inserted.site, inserted.pnome, inserted.pno, inserted.cxstamp, inserted.cxusername
, inserted.ssstamp, inserted.ssusername,isnull(fo2.operext, 0)
from inserted
inner join cm1 on cm1.cm=inserted.doccode
inner join oc on oc.olcodigo=cm1.olfl
inner join fo2 on fo2.fo2stamp=inserted.fostamp
where cm1.folanol =1
and not exists(select fostamp from ol where ol.fostamp=inserted.fostamp)
and fo2.anulado =0
END

IF (UPDATE(tpdesc) OR UPDATE(data) OR UPDATE(no) OR UPDATE(nome)
OR UPDATE(doccode) OR UPDATE(docnome) OR UPDATE(moeda) OR UPDATE(adoc) OR UPDATE(pdata) OR UPDATE(fref) OR UPDATE(cambiofixo) OR UPDATE(ccusto)
OR UPDATE(ncusto) OR UPDATE(intid) OR UPDATE(tipo) OR UPDATE(estab) OR UPDATE(pais) OR UPDATE(zona) OR UPDATE(aprovado) OR UPDATE(nmaprov) OR UPDATE(dtaprov)
OR UPDATE(ivav1) OR UPDATE(ivav2) OR UPDATE(ivav3) OR UPDATE(ivav4) OR UPDATE(ivav5) OR UPDATE(ivav6) OR UPDATE(ivav7) OR UPDATE(ivav8) OR UPDATE(ivav9)
OR UPDATE(eivav1) OR UPDATE(eivav2) OR UPDATE(eivav3) OR UPDATE(eivav4) OR UPDATE(eivav5) OR UPDATE(eivav6) OR UPDATE(eivav7) OR UPDATE(eivav8) OR UPDATE(eivav9)
OR UPDATE(virs) OR UPDATE(evirs) OR UPDATE(total) OR UPDATE(ttiva) OR UPDATE(etotal) OR UPDATE(ettiva) OR UPDATE(totmoeda) OR UPDATE(tmiva)
OR UPDATE(crend) OR UPDATE(final) OR UPDATE(multi))
BEGIN

update FC set
debf =(case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end )
, edebf =(case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end )
, debfm =(case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end)
, credf =(case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end)
, ecredf =(case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end)
, credfm =(case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end)
, virsreg =(case when cm1.debito=1 then inserted.virs*-1 else inserted.virs end)
, evirsreg =(case when cm1.debito=1 then inserted.evirs*-1 else inserted.evirs end)
, obs =inserted.final
, tiporend = fo2.tiporend
from inserted
inner join cm1 on cm1.cm =inserted.doccode
inner join fo2 on fo2.fo2stamp =inserted.fostamp
inner join fc on fc.fostamp =inserted.fostamp
where cm1.folanfc =1 and cm1.fcautoreg =1

insert into FC
(tpdesc, tpstamp, fcstamp, fostamp, usrdata, usrhora, usrinis, ousrdata, ousrhora, ousrinis
, datalc, no, nome, cm, cmdesc, origem
, cred, ecred, deb, edeb, credm,debm, credf, ecredf, debf, edebf, credfm, debfm
, aprovado, nmaprov, dtaprov, ivav1, ivav2, ivav3, ivav4, ivav5, ivav6, ivav7, ivav8, ivav9
, eivav1, eivav2, eivav3, eivav4, eivav5, eivav6, eivav7, eivav8, eivav9
, ivatx1, ivatx2, ivatx3, ivatx4, ivatx5, ivatx6, ivatx7, ivatx8, ivatx9, reexgiva
, datasup12, formapag, moeda, adoc, dataven, fref, cambiofixo, ccusto, ncusto, intid, tipo, estab, pais, zona, crend, obs
, tiporend
)
select inserted.tpdesc, inserted.tpstamp
, substring('R'+inserted.fostamp,1,25), inserted.fostamp, inserted.usrdata, inserted.usrhora, inserted.usrinis
, inserted.ousrdata, inserted.ousrhora, inserted.ousrinis, inserted.data, inserted.no, inserted.nome,cm1.cmfcautoreg
, cm1.cmfcautoregn, 'FO'
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) else 0 end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.total-(case when inserted.pais=2 then inserted.ttiva else (isnull(fo2.ttinvsuj,0)) end) else inserted.total end ) end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ettinvsuj<>0 then inserted.etotal-(case when inserted.pais=2 then inserted.ettiva else (isnull(fo2.ettinvsuj,0)) end) else inserted.etotal end ) end
, case when cm1.debito=1 then ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) else 0 end
, case when cm1.debito=1 then 0 else ( case when inserted.pais=2 or fo2.ttinvsuj<>0 then inserted.totmoeda-inserted.tmiva else inserted.totmoeda end ) end
, inserted.aprovado, inserted.nmaprov, inserted.dtaprov
, inserted.ivav1 - fo2.ivainvsuj1, inserted.ivav2 - fo2.ivainvsuj2, inserted.ivav3 - fo2.ivainvsuj3
, inserted.ivav4 - fo2.ivainvsuj4, inserted.ivav5 - fo2.ivainvsuj5, inserted.ivav6 - fo2.ivainvsuj6
, inserted.ivav7 - fo2.ivainvsuj7, inserted.ivav8 - fo2.ivainvsuj8, inserted.ivav9 - fo2.ivainvsuj9
, inserted.eivav1 - fo2.eivainvsuj1, inserted.eivav2 - fo2.eivainvsuj2, inserted.eivav3 - fo2.eivainvsuj3
, inserted.eivav4 - fo2.eivainvsuj4, inserted.eivav5 - fo2.eivainvsuj5, inserted.eivav6 - fo2.eivainvsuj6
, inserted.eivav7 - fo2.eivainvsuj7, inserted.eivav8 - fo2.eivainvsuj8, inserted.eivav9 - fo2.eivainvsuj9
, isnull(fo2.ivatx1,0), isnull(fo2.ivatx2,0),isnull(fo2.ivatx3,0), isnull(fo2.ivatx4,0), isnull(fo2.ivatx5,0)
, isnull(fo2.ivatx6,0), isnull(fo2.ivatx7,0), isnull(fo2.ivatx8,0), isnull(fo2.ivatx9,0), isnull(fo2.reexgiva,0)
, dateadd(Month,13, inserted.docdata-DAY(inserted.docdata))
, isnull(fo2.formapag,'1'), inserted.moeda, inserted.adoc, inserted.pdata, inserted.fref, inserted.cambiofixo
, inserted.ccusto, inserted.ncusto, inserted.intid, inserted.tipo
, inserted.estab, inserted.pais, inserted.zona, inserted.crend, inserted.final
, fo2.tiporend
from inserted
inner join cm1 on cm1.cm =inserted.doccode
inner join fo2 on fo2.fo2stamp =inserted.fostamp
where cm1.folanfc =1 and cm1.fcautoreg =1 and inserted.multi =0
END

update FC set
tiporend = fo2.tiporend
from inserted
inner join fo2 on fo2.fo2stamp =inserted.fostamp
inner join fc on fc.fostamp =inserted.fostamp


if (update(aprovado) or update(multi))
update fc set
aprovado =i.aprovado
, nmaprov =i.nmaprov
, dtaprov =i.dtaprov
from inserted i
where fc.fostamp =i.fostamp

if (select count(*)
from inserted
inner join fc on fc.fostamp = inserted.fostamp
inner join fo2 on fo2.fo2stamp = inserted.fostamp
where fo2.anulado=1) >0
update FC set
deb = 0, edeb = 0, debf = 0, edebf = 0,debfm = 0,
cred = 0, ecred = 0, credf = 0, ecredf = 0, credfm = 0,
ivav1 = 0, ivav2 = 0, ivav3 = 0, ivav4 = 0, ivav5 = 0, ivav6 = 0, ivav7 = 0, ivav8 = 0, ivav9 = 0,
eivav1 = 0, eivav2 = 0, eivav3 = 0, eivav4 = 0, eivav5 = 0, eivav6 = 0, eivav7 = 0, eivav8 = 0, eivav9 = 0,
ivatx1 = 0, ivatx2 = 0, ivatx3 = 0, ivatx4 = 0, ivatx5 = 0, ivatx6 = 0, ivatx7 = 0, ivatx8 = 0, ivatx9 = 0,
reexgiva = 0, virs = 0, evirs = 0,
virsreg = 0, evirsreg = 0,
difcambio = 0, edifcambio = 0
from inserted
inner join fo2 on fo2.fo2stamp=inserted.fostamp
inner join fc on fc.fostamp = inserted.fostamp
where fo2.anulado = 1
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ml_delete ON ml WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONdeclare @fvar int select @fvar = 15 while @fvar>=1BEGINUPDATE PCSA SET pcsa.deb=pcsa.deb-isnull((select sum(deleted.deb) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0),pcsa.cre=pcsa.cre-isnull((select sum(deleted.cre) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0), pcsa.edeb=pcsa.edeb-isnull((select sum(deleted.edeb) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0),pcsa.ecre=pcsa.ecre-isnull((select sum(deleted.ecre) from deleted where substring(deleted.conta,1,@fvar)=pcsa.conta and year(deleted.data)=pcsa.ano and deleted.mes=pcsa.mes),0) from deleted where pcsa.conta=substring(deleted.conta,1,@fvar) and pcsa.ano=year(deleted.data) and pcsa.mes=deleted.mes and @fvar<=len(rtrim(deleted.conta)) select @fvar = @fvar -1  ENDif (select count(*) from deleted inner join gt on gt.mlstamp=deleted.mlstamp)>0 update gt set gt.mlstamp='',gt.lancado=0 from deleted where gt.mlstamp=deleted.mlstamp 		
							if not exists (select 1 from ml (nolock) inner join deleted on deleted.dostamp=ml.dostamp)
				begin
			
			DECLARE @Tabela     Varchar(20)
			DECLARE @NomeCampoStamp CHAR(15)
			DECLARE @OriStamp 	CHAR(25)
			DECLARE @Campo      CHAR(20)
			
			DECLARE @Diario		varchar(20)
			DECLARE @Data       varchar(20)
			DECLARE @Nome     	varchar(40)
			
			DECLARE @SQLString   NVARCHAR(500)
			DECLARE @SQLString2  NVARCHAR(500)
			
			DECLARE @SQLString3  NVARCHAR(2000)
			DECLARE @SQLString4  NVARCHAR(2000)
			
			DECLARE deleted_cursor CURSOR FOR 
			SELECT distinct origem,oristamp,rtrim(ltrim(origem))+'STAMP' as NomeCampoStamp,'Plano' as Campo,do.dilno as diario,do.data as data,do.dinome as nome 
			FROM deleted 
			inner join do on do.dostamp=deleted.dostamp 
			WHERE origem <> 'DO' AND origem <> '' AND origem <> 'ECAJ' AND origem <> 'ECIC' AND (oristamp <> '' or (oristamp='' and origem='PR'))
 
			OPEN deleted_cursor 

			FETCH NEXT FROM deleted_cursor
				INTO @Tabela, @OriStamp,@NomeCampoStamp, @Campo, @Diario, @Data, @Nome
			WHILE @@FETCH_STATUS = 0
			BEGIN
				IF upper(@tabela) = 'SL' SET @Campo = 'poc'
				
				
 IF upper(@tabela) = 'FT'
 BEGIN
 SET Context_Info 0x55555
 END
 IF upper(@tabela) = 'RD'
 BEGIN
 SET Context_Info 0x77777
 END
				
				IF upper(@tabela) = 'PR'
					begin
					    SET  @SQLString2 = 'UPDATE ' + @tabela + ' SET '+  @campo + '=0  WHERE dilnoplano = ''' + @Diario + ''' and dinoplano =''' + @Nome + ''' and dplano= ''' + @Data + ''''
						EXEC sp_executesql @SQLString2 
					
						 set @SQLString3 = 'update anexos set marcadoenviar = 1 from ' + @Tabela + ' t  where t.dplano = ''' + @Data + ''' and t.dinoplano = ''' + @Nome + ''' and t.dilnoplano = ''' + @Diario + '''  and anexos.oritable = ''' + @Tabela + ''' and anexos.recstamp = t.' + @NomeCampoStamp + '  and exists(select 1 from anexos a2 where a2.oritable = ''' + @Tabela + ''' and a2.recstamp = t.' + @NomeCampoStamp + ')';

 exec sp_executesql @SQLString3
					end
				else
					begin
						SET @SQLString = 'UPDATE '+ @tabela +' SET '+ @campo + '=0  WHERE ' + @NomeCampoStamp + ' = '''+@OriStamp+''''
						EXEC sp_executesql @SQLString
						
						 set @SQLString4 = 'update anexos set marcadoenviar = 1 from ' + @Tabela + ' t  where t.' + @NomeCampoStamp + ' = '''+@OriStamp+''' and anexos.oritable = ''' + @Tabela + ''' and anexos.recstamp = t.' + @NomeCampoStamp + '  and exists(select 1 from anexos a2 where a2.oritable = ''' + @Tabela + ''' and a2.recstamp = t.' + @NomeCampoStamp + ')';

 exec sp_executesql @SQLString4
					end
				
				
 IF upper(@tabela) = 'FT' or upper(@tabela) = 'RD'
 BEGIN
 SET Context_Info 0x66666
 END 
				
				FETCH NEXT FROM deleted_cursor
					INTO @Tabela, @OriStamp, @NomeCampoStamp, @Campo, @Diario, @Data, @Nome

			END
			CLOSE deleted_cursor
			DEALLOCATE deleted_cursor
			
			END
			GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_boma_delete ON boma WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONif (select count(*) from deleted where deleted.emconf=0)>0 BEGIN IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='FL') >0 BEGIN UPDATE MA SET ma.casa=1,ma.retfor=0,ma.armfornout='',ma.flnomea='',ma.flnoa=0,ma.flestab=0,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.bostampv='',ma.ftorigem='' from deleted where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='FL' and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=1) >0 BEGIN UPDATE MA SET ma.casa=0,ma.retfor=1,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted where deleted.mastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0) >0 BEGIN DELETE FROM MA FROM deleted where deleted.bomastamp=ma.bomastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and not deleted.ctrmastamp='') >0 BEGIN UPDATE MA SETma.indisponivel=0 from deleted where deleted.ctrmastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='FL' and deleted.retequi=0 and not deleted.ctrmastamp='' and deleted.emconf=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=0) >0 BEGIN UPDATE MA SET ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.nome='',ma.no=0,ma.estab=0,ma.morada='',ma.local='',ma.codpost='',ma.zona='',ma.vendnm='', ma.vendedor=0,ma.instal='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.telefone='',ma.contacto='',ma.email='', ma.ftorigem='',ma.casa=1,ma.bostampv='' from deleted where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=0 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=1) >0 BEGIN UPDATE MA SET ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)),ma.casa=1,ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata=''  from deleted where deleted.mastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and deleted.retequi=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=1 and deleted.bdemp='CL') >0 BEGIN UPDATE MA SET ma.casa=0,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted where deleted.mastamp=ma.mastamp and deleted.foserie=1 and deleted.bdemp='CL' and deleted.emconf=0 DELETE FROM MA FROM deleted where ma.bomastamp=deleted.bomastamp and deleted.mastamp='' and deleted.foserie=1 and deleted.bdemp='CL' and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=0 and deleted.refprod=1)>0 BEGIN UPDATE MA SET ma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.casa=1,ma.indisponivel=0,ma.ftorigem='',ma.bostampv='',ma.ftnmdoc='',ma.ftfno=0,ma.ftfdata='',ma.nome=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.nome end,ma.no=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.no end,ma.morada=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.morada end,ma.local=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.local end,ma.codpost=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.codpost end,ma.zona=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.zona end,ma.estab=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.estab end,ma.vendnm=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.vendnm end,ma.vendedor=case when deleted.bdemp='CL' and ts.actprodclc=1 then 0 else ma.vendedor end,ma.telefone=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.telefone end,ma.contacto=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.contacto end,ma.email=case when deleted.bdemp='CL' and ts.actprodclc=1 then '' else ma.email end,ma.instal=case when deleted.bdemp='CL' then '' else ma.instal end,ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join bo on deleted.bostamp=bo.bostamp inner join ts on bo.ndos=ts.ndos where deleted.mastamp=ma.mastamp and deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=0 and deleted.refprod=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=1 and deleted.refprod=1) >0 BEGIN DELETE FROM MA FROM deleted where ma.bomastamp=deleted.bomastamp and deleted.mastamp='' and deleted.foserie=0 and deleted.ftserie=0 and deleted.trfarm=0 and deleted.producao=1 and deleted.refprod=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.trfarm=1)>0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.noarm=deleted.armazem,ma.armdatain=deleted.dataobra, ma.obs=substring(ma.obs,1,patindex('%'+deleted.obs+'%',ma.obs)-(case when patindex('%'+deleted.obs+'%',ma.obs)>0 then 1 else 0 end)) +substring(ma.obs,patindex('%'+deleted.obs+'%',ma.obs)+LEN(deleted.obs)+LEN(convert(char(10),deleted.dataobra,121))+4,datalength(ma.obs)) from deleted inner join bo on deleted.bostamp=bo.bostamp where deleted.mastamp=ma.mastamp and deleted.trfarm=1 and deleted.emconf=0 END IF (select count(*) from deleted where deleted.ftserie=1 and deleted.bdemp='CL' and not deleted.ctrmastamp='') >0 BEGIN UPDATE MA SETma.usrinis=deleted.usrinis,ma.usrhora=deleted.usrhora,ma.usrdata=deleted.usrdata,ma.nome=deleted.nome,ma.no=deleted.no, ma.morada=bo.morada,ma.local=bo.local,ma.codpost=bo.codpost,ma.zona=bo.zona,ma.estab=bo.estab,ma.vendnm=bo.vendnm,ma.vendedor=bo.vendedor, ma.telefone=cl.telefone,ma.contacto=cl.contacto,ma.email=cl.email from deleted inner join bo on deleted.bostamp=bo.bostamp inner join cl on cl.no=bo.no and cl.estab=bo.estab where deleted.ctrmastamp=ma.mastamp and deleted.ftserie=1 and deleted.bdemp='CL' and not deleted.ctrmastamp='' and ma.no=0 and deleted.emconf=0 END IF (select COUNT(*) from deleted where deleted.serprvef = 1) > 0 UPDATE MA SET ma.serie = deleted.maserierpl from deleted where deleted.serprvef = 1 and deleted.mastamp=ma.mastamp END 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_anexos_insert ON Anexos WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ON	    DECLARE @allowedTables NVARCHAR(MAX) = 'FT,BO,FO,RD,RE,PD,OW';
		DECLARE @oritable NVARCHAR(80), 
				@anexosstamp NVARCHAR(25), 
		        @recstamp NVARCHAR(25), 
		        @original BIT,  
		        @tableToUpdate NVARCHAR(80),
		        @stampColumn NVARCHAR(80),
		        @sql NVARCHAR(MAX),
		        @updateFlag BIT,
		        @requiredValue BIT,
				@anuladoFieldTable NVARCHAR(80);

		-- Verifica se existem registros válidos
		IF EXISTS (SELECT 1 FROM inserted WHERE CHARINDEX(',' + UPPER(oritable) + ',', ',' + @allowedTables + ',') > 0)
		BEGIN
		    -- Declara cursor para percorrer os registros válidos em `inserted`
		    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
		    SELECT UPPER(oritable), recstamp, original, anexosstamp
		    FROM inserted
		    WHERE CHARINDEX(',' + UPPER(oritable) + ',', ',' + @allowedTables + ',') > 0;

		    OPEN cur;

		    FETCH NEXT FROM cur INTO @oritable, @recstamp, @original, @anexosstamp;

		    WHILE @@FETCH_STATUS = 0
		    BEGIN
					
		SET @anuladoFieldTable = CASE
									WHEN @oritable = 'BO' THEN 'bo2'
									WHEN @oritable = 'FO' THEN 'fo2'
									WHEN @oritable = 'FT' OR @oritable = 'RD' OR @oritable = 'RE' THEN @oritable
									ELSE ''
								END;

		-- Define a tabela e coluna stamp com base em `oritable`
		SET @tableToUpdate = CASE
		                        WHEN @oritable = 'FT' THEN 'ft3'
		                        WHEN @oritable = 'BO' THEN 'bo3'
		                        WHEN @oritable = 'FO' THEN 'fo2'
		                        ELSE @oritable
		                     END;

		SET @stampColumn = CASE
		                        WHEN @oritable = 'FT' THEN 'ft3stamp'
		                        WHEN @oritable = 'BO' THEN 'bo3stamp'
		                        WHEN @oritable = 'FO' THEN 'fo2stamp'
		                        ELSE @oritable + 'stamp'
		                   END;

		-- Inicializa a flag de atualização como 0
		SET @updateFlag = 0;

		-- Verificações específicas por tabela
		IF @oritable = 'BO'
		BEGIN
		    IF EXISTS (SELECT 1 FROM bo INNER JOIN ts ON bo.ndos = ts.ndos
		              WHERE ts.tiposaft NOT IN ('', '--') AND bo.bostamp = @recstamp)
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'FT'
		BEGIN
		    IF EXISTS (SELECT 1 FROM ft INNER JOIN td ON ft.ndoc = td.ndoc
		              WHERE (CASE WHEN ((td.docsimport = 0 AND td.docsext = 0) OR td.docsext = 1) THEN 0 ELSE td.excluisaft END = 0)
		              AND td.tiposaft NOT IN ('', '--') AND ft.ftstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'OW'
		BEGIN
		    IF EXISTS (SELECT 1 FROM ow WHERE (ow.ozstamp = '' AND ow.prstamp = '' AND ow.frstamp = '' AND ow.vrstamp = '' AND ow.postamp = '' AND ow.restamp = '')
		              AND ow.owstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'RD'
		BEGIN
		    IF EXISTS (SELECT 1 FROM rd INNER JOIN tsrd ON rd.ndoc = tsrd.ndoc
		              WHERE tsrd.tiposaft NOT IN ('', '--') AND rd.rdstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'RE'
		BEGIN
		    IF EXISTS (SELECT 1 FROM re INNER JOIN tsre ON re.ndoc = tsre.ndoc
		              WHERE tsre.movinc = 0 AND re.restamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE
		BEGIN
		    -- Para as demais tabelas, atualiza sem condições adicionais
		    SET @updateFlag = 1;
		END

		        -- Se a flag de atualização for 1, continua com a lógica para atualizar o campo ArquivadoDigital
		        IF @updateFlag = 1
		        BEGIN
		        	IF @anuladoFieldTable <> ''
						BEGIN
				        	SET @sql = '
				        		IF EXISTS (
							            SELECT 1 
							            FROM ' + QUOTENAME(@anuladoFieldTable) + '
							            INNER JOIN anexos ON anexos.recstamp = ' + QUOTENAME(@anuladoFieldTable) + '.' + QUOTENAME(@anuladoFieldTable + 'STAMP') + '
							            AND anexos.anexosstamp = @anexosstamp
							            WHERE anulado = 1)
						        BEGIN 
						            UPDATE anexos 
							        SET eanulado = 1 
							        WHERE anexosstamp = @anexosstamp; 
						        END;';
						END
					
						EXEC sp_executesql @sql, 
						     N'@anexosstamp NVARCHAR(25)',
						     @anexosstamp = @anexosstamp;
		        
				    -- Define o valor necessário para ArquivadoDigital
				    BEGIN
					    SET @sql = '
					        SELECT @result = CASE 
					            WHEN EXISTS (
					                SELECT 1 FROM anexos ';

					    -- Conditionally add the LEFT JOIN if the table is provided
					    IF @anuladoFieldTable <> ''
					    BEGIN
					        SET @sql = @sql + '
					                LEFT JOIN ' + QUOTENAME(@anuladoFieldTable) + ' 
					                ON ' + QUOTENAME(@anuladoFieldTable) + '.' + QUOTENAME(@anuladoFieldTable + 'STAMP') + ' = @recstamp ';
					    END

					    SET @sql = @sql + '
					                WHERE oritable = @oritable 
					                  AND recstamp = @recstamp 
					                  AND original = 1 ';

					    -- Conditionally add the anulado condition if the table is provided
					    IF @anuladoFieldTable <> ''
					    BEGIN
					        SET @sql = @sql + '
					                  AND (' + QUOTENAME(@anuladoFieldTable) + '.anulado <> 1 
					                       OR (' + QUOTENAME(@anuladoFieldTable) + '.anulado = 1 AND eanulado = 1))';
					    END

					    SET @sql = @sql + '
					            ) THEN 1
					            ELSE 0
					        END;';
					END

				    -- Execute dynamic SQL to retrieve @result value
				    EXEC sp_executesql @sql, 
				        N'@recstamp NVARCHAR(25), @oritable NVARCHAR(80), @result BIT OUTPUT',  
				        @recstamp = @recstamp, 
				        @oritable = @oritable, 
				        @result = @requiredValue OUTPUT;

				    -- Create the SQL dynamic command for updating
				    SET @sql = N'
				        IF EXISTS (
				            SELECT 1 FROM ' + QUOTENAME(@tableToUpdate) + ' 
				            WHERE ' + QUOTENAME(@stampColumn) + ' = @recstamp 
				            AND ArquivadoDigital <> @requiredValue
				        ) 
				        BEGIN 
				            UPDATE ' + QUOTENAME(@tableToUpdate) + ' 
				            SET ArquivadoDigital = @requiredValue 
				            WHERE ' + QUOTENAME(@stampColumn) + ' = @recstamp; 
				        END;';

				    -- Execute the dynamic SQL update
				    EXEC sp_executesql @sql, 
				        N'@recstamp NVARCHAR(25), @requiredValue BIT', 
				        @recstamp = @recstamp, 
				        @requiredValue = @requiredValue;
				END;


		        FETCH NEXT FROM cur INTO @oritable, @recstamp, @original, @anexosstamp;
		    END

		    CLOSE cur;
		    DEALLOCATE cur;
		END
    GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_anexos_delete ON Anexos WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ON		DECLARE @allowedTables NVARCHAR(MAX) = 'FT,BO,FO,RD,RE,PD,OW';
		DECLARE @oritable NVARCHAR(80), 
		        @anexosstamp NVARCHAR(25), 
		        @recstamp NVARCHAR(25), 
		        @original BIT,  
		        @tableToUpdate NVARCHAR(80),
		        @stampColumn NVARCHAR(80),
		        @sql NVARCHAR(MAX),
		        @updateFlag BIT,
		        @requiredValue BIT,
				@anuladoFieldTable NVARCHAR(80);

		-- Verifica se existem registros válidos
		IF EXISTS (SELECT 1 FROM deleted WHERE CHARINDEX(',' + UPPER(oritable) + ',', ',' + @allowedTables + ',') > 0)
		BEGIN
		    -- Declara cursor para percorrer os registros válidos em `deleted`
		    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR    
		    SELECT UPPER(oritable), recstamp, original, anexosstamp
		    FROM deleted
		    WHERE CHARINDEX(',' + UPPER(oritable) + ',', ',' + @allowedTables + ',') > 0;

		    OPEN cur;

		    FETCH NEXT FROM cur INTO @oritable, @recstamp, @original, @anexosstamp;

		    WHILE @@FETCH_STATUS = 0
		    BEGIN
					
		SET @anuladoFieldTable = CASE
									WHEN @oritable = 'BO' THEN 'bo2'
									WHEN @oritable = 'FO' THEN 'fo2'
									WHEN @oritable = 'FT' OR @oritable = 'RD' OR @oritable = 'RE' THEN @oritable
									ELSE ''
								END;

		-- Define a tabela e coluna stamp com base em `oritable`
		SET @tableToUpdate = CASE
		                        WHEN @oritable = 'FT' THEN 'ft3'
		                        WHEN @oritable = 'BO' THEN 'bo3'
		                        WHEN @oritable = 'FO' THEN 'fo2'
		                        ELSE @oritable
		                     END;

		SET @stampColumn = CASE
		                        WHEN @oritable = 'FT' THEN 'ft3stamp'
		                        WHEN @oritable = 'BO' THEN 'bo3stamp'
		                        WHEN @oritable = 'FO' THEN 'fo2stamp'
		                        ELSE @oritable + 'stamp'
		                   END;

		-- Inicializa a flag de atualização como 0
		SET @updateFlag = 0;

		-- Verificações específicas por tabela
		IF @oritable = 'BO'
		BEGIN
		    IF EXISTS (SELECT 1 FROM bo INNER JOIN ts ON bo.ndos = ts.ndos
		              WHERE ts.tiposaft NOT IN ('', '--') AND bo.bostamp = @recstamp)
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'FT'
		BEGIN
		    IF EXISTS (SELECT 1 FROM ft INNER JOIN td ON ft.ndoc = td.ndoc
		              WHERE (CASE WHEN ((td.docsimport = 0 AND td.docsext = 0) OR td.docsext = 1) THEN 0 ELSE td.excluisaft END = 0)
		              AND td.tiposaft NOT IN ('', '--') AND ft.ftstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'OW'
		BEGIN
		    IF EXISTS (SELECT 1 FROM ow WHERE (ow.ozstamp = '' AND ow.prstamp = '' AND ow.frstamp = '' AND ow.vrstamp = '' AND ow.postamp = '' AND ow.restamp = '')
		              AND ow.owstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'RD'
		BEGIN
		    IF EXISTS (SELECT 1 FROM rd INNER JOIN tsrd ON rd.ndoc = tsrd.ndoc
		              WHERE tsrd.tiposaft NOT IN ('', '--') AND rd.rdstamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE IF @oritable = 'RE'
		BEGIN
		    IF EXISTS (SELECT 1 FROM re INNER JOIN tsre ON re.ndoc = tsre.ndoc
		              WHERE tsre.movinc = 0 AND re.restamp = @recstamp)
		
		        SET @updateFlag = 1;
		END
		ELSE
		BEGIN
		    -- Para as demais tabelas, atualiza sem condições adicionais
		    SET @updateFlag = 1;
		END

		        -- Se a flag de atualização for 1, continua com a lógica para atualizar o campo ArquivadoDigital
		        IF @updateFlag = 1
		        BEGIN
		            -- Define o valor necessário para ArquivadoDigital
		            BEGIN
					    SET @sql = '
					        SELECT @result = CASE 
					            WHEN EXISTS (
					                SELECT 1 FROM anexos ';

					    -- Conditionally add the LEFT JOIN if the table is provided
					    IF @anuladoFieldTable <> ''
					    BEGIN
					        SET @sql = @sql + '
					                LEFT JOIN ' + QUOTENAME(@anuladoFieldTable) + ' 
					                ON ' + QUOTENAME(@anuladoFieldTable) + '.' + QUOTENAME(@anuladoFieldTable + 'STAMP') + ' = @recstamp ';
					    END

					    SET @sql = @sql + '
					                WHERE oritable = @oritable 
					                  AND recstamp = @recstamp 
					                  AND original = 1 ';

					    -- Conditionally add the anulado condition if the table is provided
					    IF @anuladoFieldTable <> ''
					    BEGIN
					        SET @sql = @sql + '
					                  AND (' + QUOTENAME(@anuladoFieldTable) + '.anulado <> 1 
					                       OR (' + QUOTENAME(@anuladoFieldTable) + '.anulado = 1 AND eanulado = 1))';
					    END

					    SET @sql = @sql + '
					            ) THEN 1
					            ELSE 0
					        END;';
					END

						EXEC sp_executesql @sql, 
							N'@recstamp NVARCHAR(25), @oritable NVARCHAR(80), @anexosstamp NVARCHAR(25), @result BIT OUTPUT', 
							@recstamp = @recstamp, 
							@oritable = @oritable, 
							@anexosstamp = @anexosstamp, 
							@result = @requiredValue OUTPUT;

		            -- Cria o comando SQL dinâmico para atualização
		            SET @sql = 'IF EXISTS (SELECT 1 FROM ' + QUOTENAME(@tableToUpdate) +
		                       ' WHERE ' + QUOTENAME(@stampColumn) + ' = @recstamp AND ArquivadoDigital <> @requiredValue) ' +
		                       'BEGIN ' +
		                           'UPDATE ' + QUOTENAME(@tableToUpdate) +
		                           ' SET ArquivadoDigital = @requiredValue ' +
		                           'WHERE ' + QUOTENAME(@stampColumn) + ' = @recstamp; ' +
		                       'END;';

		            -- Executa o SQL dinâmico para cada registro individualmente
		            EXEC sp_executesql @sql, 
		                N'@recstamp NVARCHAR(25), @requiredValue BIT', 
		                @recstamp = @recstamp, 
		                @requiredValue = @requiredValue;
		        END

		        FETCH NEXT FROM cur INTO @oritable, @recstamp, @original, @anexosstamp;
		    END

		    CLOSE cur;
		    DEALLOCATE cur;
		END
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_rl_update ON rl WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONUPDATE CC SET
cc.debf=cc.debf-ISNULL((select case when sum(deleted.cambio)= 0 and Sum(deleted.escrec)>0 then sum(deleted.escrec)-sum(deleted.arred) else
case when Sum(deleted.cambio)<>0 and Sum(deleted.escrec)>0 then Sum(deleted.escrec) else 0 end end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.credf=cc.credf+ISNULL((select case when sum(deleted.cambio)= 0 and Sum(deleted.escrec)<0 then sum(deleted.escrec)-sum(deleted.arred) else
case when Sum(deleted.cambio)<>0 and Sum(deleted.escrec)<0 then Sum(deleted.escrec) else 0 end end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.debfm=cc.debfm-ISNULL((select case when sum(deleted.ecambio)<>0 and Sum(deleted.rec)>0 then sum(deleted.rec) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.credfm=cc.credfm+ISNULL((select case when sum(deleted.ecambio)<>0 and Sum(deleted.rec)<0 then sum(deleted.rec) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.edebf=cc.edebf-ISNULL((select case when sum(deleted.erec)>0 then sum(deleted.erec)-sum(deleted.earred) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.ecredf=cc.ecredf+ISNULL((select case when sum(deleted.erec)<0 then sum(deleted.erec)-sum(deleted.earred) else 0 end
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0)
, cc.ivacatreg =cc.ivacatreg -ISNULL((select sum(abs(deleted.ivacativado)) from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1), 0)
, cc.eivacatreg =cc.eivacatreg -ISNULL((select sum(abs(deleted.eivacativado)) from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1), 0)
, cc.mivacatreg =cc.mivacatreg -ISNULL((select sum(abs(deleted.mivacativado)) from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1), 0)
, cc.virsreg=cc.virsreg-ISNULL((select sum(deleted.virs)
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0),
cc.evirsreg=cc.evirsreg-ISNULL((select sum(deleted.evirs)
 from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1
), 0)
from deleted where deleted.ccstamp=cc.ccstamp and deleted.process=1

UPDATE CC
set cc.ultdoc=''
, cc.recibado=0,cc.recino=0,cc.recian=0
, cc.recindoc=0
, cc.valre =cc.valre -deleted.escrec + deleted.ivacativado
, cc.evalre =cc.evalre -deleted.erec + deleted.eivacativado
, cc.mvalre =cc.mvalre -deleted.rec + deleted.mivacativado
, cc.valch = ISNULL(( select case when ISNULL(sum(re.total),0) <> 0 then round(((sum(rl.escrec)*100/sum(re.total))/100)*(sum(re.chtotal)),2)
else 0 end
from re	
inner join rl on rl.restamp = re.restamp
where rl.ccstamp = deleted.ccstamp and re.restamp <> deleted.restamp and deleted.cheque = 1),0)
, cc.evalch = isnull((
select case when ISNULL(sum(re.etotal),0)<> 0 then round(((sum(rl.erec)*100/sum(re.etotal))/100)*(sum(re.echtotal)),2)
else 0 end
from re
inner join rl on rl.restamp = re.restamp
where rl.ccstamp = deleted.ccstamp and re.restamp <> deleted.restamp and deleted.cheque = 1),0)
, cc.cbbno=0
from deleted
where cc.ccstamp =deleted.ccstamp

UPDATE CC 
set cc.irsdif =cc.irsdif-case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.virs/(1-(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),0)-deleted.virs) end
, cc.eirsdif =cc.eirsdif-case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.evirs/(1-(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),2)-deleted.evirs) end
, cc.ivacatdif =cc.ivacatdif -case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.ivacativado /(1 -(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),0)-deleted.ivacativado) end
, cc.eivacatdif =cc.eivacatdif -case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.eivacativado /(1 -(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),2)-deleted.eivacativado) end
, cc.mivacatdif = cc.mivacatdif -case when deleted.desconto=100 or re.fin=100 then 0 
else (ROUND((deleted.mivacativado /(1 -(case when deleted.desconto<>0 then deleted.desconto 
else re.fin end/100))),2)-deleted.mivacativado) end
from deleted
inner join re on re.restamp=deleted.restamp
where cc.ccstamp=deleted.ccstamp and deleted.process=1 and (deleted.desconto<>0 or re.fin<>0)if (select count(*) from deleted inner join re on re.restamp=deleted.restamp inner join tsre (nolock) on deleted.ndoc=tsre.ndoc where tsre.intropag=1)>0 UPDATE RD set rd.integrado=(case when (select count(*) from cc (nolock) inner join rd (nolock) on rd.rdstamp=cc.rdstamp where cc.ccstamp=deleted.ccstamp)=1 then 0 else 1 end) from deleted inner join cc (nolock) on cc.ccstamp = deleted.ccstamp inner join rd (nolock) on rd.rdstamp = cc.rdstamp UPDATE CC SET
cc.debf=cc.debf+ISNULL((select case when sum(inserted.ecambio)=0 and Sum(inserted.escrec)-Sum(inserted.arred)>0 then Sum(inserted.escrec)-Sum(inserted.arred) else
case when Sum(inserted.cambio)<>0 and Sum(inserted.escrec)>0 then Sum(inserted.escrec) else 0 end end
from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.credf=cc.credf-ISNULL((select case when sum(inserted.ecambio)=0 and Sum(inserted.escrec)-Sum(inserted.arred)<0 then Sum(inserted.escrec)-Sum(inserted.arred) else
case when Sum(inserted.cambio)<>0 and Sum(inserted.escrec)<0 then Sum(inserted.escrec) else 0 end end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.debfm=cc.debfm+ISNULL((select case when sum(inserted.ecambio)<>0 and Sum(inserted.rec)>0 then sum(inserted.rec) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.credfm=cc.credfm-ISNULL((select case when sum(inserted.ecambio)<>0 and Sum(inserted.rec)<0 then sum(inserted.rec) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.edebf=cc.edebf+ISNULL((select case when sum(inserted.erec)>0 then sum(inserted.erec)-sum(inserted.earred) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0),
cc.ecredf =cc.ecredf-ISNULL((select case when sum(inserted.erec)<0 then sum(inserted.erec)-sum(inserted.earred) else 0 end
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.ivacatreg =cc.ivacatreg +ISNULL((select sum(abs(inserted.ivacativado))  from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.eivacatreg=cc.eivacatreg +ISNULL((select sum(abs(inserted.eivacativado)) from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.mivacatreg =cc.mivacatreg +ISNULL((select sum(abs(inserted.mivacativado))  from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.virsreg=cc.virsreg+ISNULL((select sum(inserted.virs)
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
, cc.evirsreg=cc.evirsreg+ISNULL((select sum(inserted.evirs)
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)), 0)
 from inserted where inserted.ccstamp=cc.ccstamp and inserted.process=1
 and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc)

UPDATE CC
set cc.ultdoc= case when tsre.movinc = 0 then 'Recibo nº '+convert(char(10),inserted.rno)+' de '+convert(char(10),inserted.rdata,104)
else 'Recibo Incobrável nº '+convert(char(10),inserted.rno)+' de '+convert(char(10),inserted.rdata,104) end
, cc.recibado=1,cc.recino=inserted.rno,cc.recian=datepart(yy,inserted.rdata)
, cc.recindoc =inserted.ndoc
, cc.valre =cc.valre +inserted.escrec - inserted.ivacativado
, cc.evalre =cc.evalre+inserted.erec - inserted.eivacativado
, cc.mvalre =cc.mvalre+inserted.rec - inserted.mivacativado
, cc.valch = ISNULL(( select case when ISNULL(sum(re.total),0) <> 0 then round(((sum(rl.escrec)*100/sum(re.total))/100)*(sum(re.chtotal)),2)
else 0 end
from re (nolock)
inner join rl (nolock) on rl.restamp = re.restamp
where rl.ccstamp = inserted.ccstamp and inserted.cheque = 1),0)
,  cc.evalch = ISNULL(( select case when ISNULL(sum(re.etotal),0) <> 0 then round(((sum(rl.erec)*100/sum(re.etotal))/100)*(sum(re.echtotal)),2)
else 0 end
from re (nolock)
inner join rl (nolock) on rl.restamp = re.restamp
where rl.ccstamp = inserted.ccstamp and rl.cheque = 1),0)
, cc.cbbno = re.cbbno
from inserted
inner join re on re.restamp=inserted.restamp
inner join tsre on tsre.ndoc = re.ndoc
where cc.ccstamp=inserted.ccstamp
and not (cc.recino=inserted.rno and cc.recian=datepart(yy,inserted.rdata) and cc.recindoc=inserted.ndoc )
and re.anulado=0

UPDATE CC
set cc.irsdif =cc.irsdif+case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.virs/(1-(case when inserted.desconto<>0 then inserted.desconto else re.fin end/100))),0)-inserted.virs) end
, cc.eirsdif =cc.eirsdif+case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.evirs/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),2)-inserted.evirs) end
, cc.ivacatdif =cc.ivacatdif +case when inserted.desconto=100 or re.fin=100 then 0
else (round((inserted.ivacativado/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),0)-inserted.ivacativado) end
, cc.eivacatdif =cc.eivacatdif +case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.eivacativado/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),2)-inserted.eivacativado) end
, cc.mivacatdif =cc.mivacatdif +case when inserted.desconto=100 or re.fin=100 then 0
else (ROUND((inserted.mivacativado/(1-(case when inserted.desconto<>0 then inserted.desconto
else re.fin end/100))),2)-inserted.mivacativado) end
from inserted
inner join re on re.restamp=inserted.restamp
where cc.ccstamp=inserted.ccstamp
and inserted.process=1 and (inserted.desconto<>0 or re.fin<>0)

if (select count(*) 
from inserted 
inner join re (nolock) on re.restamp=inserted.restamp
inner join tsre (nolock) on inserted.ndoc=tsre.ndoc 
where tsre.intropag=1)>0
UPDATE RD 
set rd.integrado=1 
from inserted
inner join cc (nolock) on cc.ccstamp = inserted.ccstamp
inner join rd (nolock) on rd.rdstamp = cc.rdstampGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bi_delete ON bi WITH ENCRYPTION FOR DELETE  AS SET NOCOUNT ONif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado=0) BEGIN DECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTIF @@MROW = 1BEGIN if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and bo2.anulado=0) BEGINUPDATE ST SET st.qttcli=st.qttcli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and st.ref=deleted.ref and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0UPDATE SA SET sa.rescli =sa.rescli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and bo2.anulado=0) BEGINUPDATE ST SET st.qttcat=st.qttcat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and st.ref=deleted.ref and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.rescat =sa.rescat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE ST SET st.qttfor=st.qttfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and st.ref=deleted.ref and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.resfor =sa.resfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resusr=1 and deleted.fechada=0 and bo2.anulado=0) UPDATE ST SET st.usrqtt=st.usrqtt-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and st.ref=deleted.ref and deleted.resusr=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE ST SET st.qttrec=st.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and st.ref=deleted.ref and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.qttrec =sa.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli=sx.rescli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat=sx.rescat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor =sx.resfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec=sx.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 END ELSE BEGIN if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.qttrec =sa.qttrec-case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.resrec=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)UPDATE st SET st.qttrec =st.qttrec-case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resrec=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescli = sa.rescli -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.rescli=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)UPDATE st SET st.qttcli = st.qttcli -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.rescli=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescat = sa.rescat -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.cativo=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)UPDATE st SET st.qttcat = st.qttcat -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.cativo=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.resfor = sa.resfor -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.resfor=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)UPDATE st SET st.qttfor = st.qttfor -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resfor=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resusr=1 and deleted.fechada=0 and bo2.anulado=0)  UPDATE st SET st.usrqtt = st.usrqtt -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resusr=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resusr=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.emconf=0 and bo2.anulado = 0)if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec =sx.qttrec-ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.resrec=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   where deleted.emconf=0 and sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli = sx.rescli - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam and deleted.fechada=0 and deleted.rescli=1),0) FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0  WHERE deleted.emconf=0 and   sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem  if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat = sx.rescat - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.cativo=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   where deleted.emconf=0 and sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor = sx.resfor - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.resfor=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   where deleted.emconf=0 and sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem END if exists(select top 1 sl.slstamp from sl inner join deleted on deleted.ref<>'' and sl.bistamp=deleted.bistamp and deleted.bistamp<>'' and deleted.emconf=0 inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado=0) DELETE FROM SL from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and sl.bistamp=deleted.bistamp and deleted.bistamp<>'' and deleted.emconf=0 and bo2.anulado = 0if exists(select top 1 bi.bistamp from bi inner join deleted on deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado=0) BEGIN UPDATE BI SET bi.fno = 0,bi.adoc = ' ',bi.nmdoc = ' ',bi.ndoc = 0, bi.qtt2 = bi.qtt2-(SELECT SUM(deleted.qtt) FROM deleted inner join bo (nolock) on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join bo on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0 and bo2.anulado = 0UPDATE BI SET bi.partes2 = bi.partes2-(SELECT SUM(deleted.partes) FROM deleted inner join bo (nolock) on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join bi (nolock) on bi.bistamp = deleted.obistamp inner join ts (nolock) on ts.ndos = bi.ndos where deleted.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and deleted.emconf=0 and bo2.anulado = 0END if exists(select top 1 biprecat.bistampfor from biprecat inner join deleted on biprecat.bistampfor=deleted.bistamp inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join ts on deleted.ndos=ts.ndos inner join ts2 on ts2.ts2stamp=ts.tsstamp where deleted.resfor=1 and ts2.precativo=1 and bo2.anulado=0) DELETE FROM BIPRECAT FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where biprecat.bistampfor=deleted.bistamp and bo2.anulado = 0if exists(select top 1 biprecat.bistampcli from biprecat inner join deleted on biprecat.bistampcli=deleted.bistamp inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join ts on deleted.ndos=ts.ndos where deleted.rescli=1 and ts.cativo=1 and bo2.anulado=0) DELETE FROM BIPRECAT FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where biprecat.bistampcli=deleted.bistamp and bo2.anulado = 0 END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_do_update ON DO WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON IF update(dilno) OR update(ano) OR update(mes) OR update(edebana) OR update(edebord) OR update(edebfin) OR update(ecreana) OR update(ecreord) OR update(ecrefin)  BEGIN UPDATE di 
SET di.edeb = di.edeb - ISNULL((SELECT SUM(deleted.edebana+deleted.edebord+deleted.edebfin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0)
, di.deb = di.deb - ISNULL((SELECT SUM(deleted.debana+deleted.debord+deleted.debfin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0) 			
, di.cre=di.cre - ISNULL((SELECT SUM(deleted.creana+deleted.creord+deleted.crefin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0) 	
, di.ecre=di.ecre - ISNULL((SELECT SUM(deleted.ecreana+deleted.ecreord+deleted.ecrefin) FROM deleted where deleted.dino = di.dino and deleted.ano = di.diano),0)  		
FROM deleted INNER JOIN di on deleted.dino=di.dino and di.diano = deleted.ano			 END IF update(dilno) OR update(ano) OR update(mes) OR update(edebana) OR update(edebord) OR update(edebfin) OR update(ecreana) OR update(ecreord) OR update(ecrefin)	
BEGIN 
UPDATE do
SET dilnoid = ISNULL(CASE WHEN anospc.di_mensal = 1 THEN inserted.mes ELSE inserted.ano END,0) 
From do inner join inserted on do.dostamp = inserted.dostamp
inner join anospc on anospc.ano = inserted.ano
UPDATE di 
SET	di.dilnoano = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano order by inserted.dilno desc),0) > di.dilnoano 
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano order by inserted.dilno desc),0) ELSE di.dilnoano 
END
, di.dilnomes00 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 0 order by inserted.dilno desc),0) > di.dilnomes00
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 0 order by inserted.dilno desc),0) ELSE di.dilnomes00
END
, di.dilnomes01 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 1 order by inserted.dilno desc),0) > di.dilnomes01
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 1 order by inserted.dilno desc),0) ELSE di.dilnomes01
END
, di.dilnomes02 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 2 order by inserted.dilno desc),0) > di.dilnomes02
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 2 order by inserted.dilno desc),0) ELSE di.dilnomes02
END
, di.dilnomes03 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 3 order by inserted.dilno desc),0) > di.dilnomes03
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 3 order by inserted.dilno desc),0) ELSE di.dilnomes03
END
, di.dilnomes04 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 4 order by inserted.dilno desc),0) > di.dilnomes04
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 4 order by inserted.dilno desc),0) ELSE di.dilnomes04
END
, di.dilnomes05 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 5 order by inserted.dilno desc),0) > di.dilnomes05
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 5 order by inserted.dilno desc),0) ELSE di.dilnomes05
END
, di.dilnomes06 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 6 order by inserted.dilno desc),0) > di.dilnomes06
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 6 order by inserted.dilno desc),0) ELSE di.dilnomes06
END
, di.dilnomes07 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 7 order by inserted.dilno desc),0) > di.dilnomes07
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 7 order by inserted.dilno desc),0) ELSE di.dilnomes07
END
, di.dilnomes08 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 8 order by inserted.dilno desc),0) > di.dilnomes08
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 8 order by inserted.dilno desc),0) ELSE di.dilnomes08
END
, di.dilnomes09 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 9 order by inserted.dilno desc),0) > di.dilnomes09
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 9 order by inserted.dilno desc),0) ELSE di.dilnomes09
END
, di.dilnomes10 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 10 order by inserted.dilno desc),0) > di.dilnomes10
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 10 order by inserted.dilno desc),0) ELSE di.dilnomes10
END
, di.dilnomes11 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 11 order by inserted.dilno desc),0) > di.dilnomes11
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 11 order by inserted.dilno desc),0) ELSE di.dilnomes11
END
, di.dilnomes12 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 12 order by inserted.dilno desc),0) > di.dilnomes12
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 12 order by inserted.dilno desc),0) ELSE di.dilnomes12
END
, di.dilnomes13 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 13 order by inserted.dilno desc),0) > di.dilnomes13
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 13 order by inserted.dilno desc),0) ELSE di.dilnomes13
END
, di.dilnomes14 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 14 order by inserted.dilno desc),0) > di.dilnomes14
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 14 order by inserted.dilno desc),0) ELSE di.dilnomes14
END
, di.dilnomes15 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 15 order by inserted.dilno desc),0) > di.dilnomes15
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 15 order by inserted.dilno desc),0) ELSE di.dilnomes15
END
, di.dilnomes16 = CASE 
WHEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 16 order by inserted.dilno desc),0) > di.dilnomes16
THEN ISNULL((select top 1 inserted.dilno FROM inserted WHERE inserted.dino=di.dino and di.diano = inserted.ano and inserted.mes = 16 order by inserted.dilno desc),0) ELSE di.dilnomes16
END 
, di.edeb = di.edeb + ISNULL((SELECT SUM(inserted.edebana+inserted.edebord+inserted.edebfin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0)
, di.deb = di.deb + ISNULL((SELECT SUM(inserted.debana+inserted.debord+inserted.debfin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0) 			
, di.cre=di.cre + ISNULL((SELECT SUM(inserted.creana+inserted.creord+inserted.crefin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0) 	
, di.ecre=di.ecre + ISNULL((SELECT SUM(inserted.ecreana+inserted.ecreord+inserted.ecrefin) FROM inserted where inserted.dino = di.dino and inserted.ano = di.diano),0)  			
FROM inserted INNER JOIN di on inserted.dino=di.dino and di.diano = inserted.ano	 update di set di.dilno=inserted.dilno+1 from inserted where inserted.dino=di.dino and inserted.dilno >= di.dilno  END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_cevi_update ON cevi WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ON		-- Verifica se a inscrição transitou de evento para outro evento
		DECLARE @DEL_stamp char(25)
		DECLARE @INS_stamp char(25)
		select @DEL_stamp = deleted.cevstamp
		from deleted
		select @INS_stamp = inserted.cevstamp
		from inserted
	    IF @DEL_stamp <> @INS_stamp
		BEGIN
			-- Remove uma inscrição do Evento a que pertencia
			UPDATE CEV
			SET	CEV.npres = CEV.npres - 1
			FROM DELETED
			WHERE CEV.cevstamp = DELETED.cevstamp
			-- Adiciona uma inscrição ao Evento a que passa a pertencer
			UPDATE CEV
			SET	CEV.npres = CEV.npres + 1
			FROM INSERTED
			WHERE CEV.cevstamp = INSERTED.cevstamp
		END
		-- Primeiro Paço
		UPDATE CEV
		SET	CEV.cont1ct = CEV.cont1ct - (CASE WHEN CEV.cont1chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont1chkval)
											  ELSE
											  	   DELETED.cont1ct
										 END
									    ),
			CEV.cont2ct = CEV.cont2ct - (CASE WHEN CEV.cont2chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont2chkval)
											  ELSE
											  	   DELETED.cont2ct
									     END
									    ),			
			CEV.cont3ct = CEV.cont3ct - (CASE WHEN CEV.cont3chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont3chkval)
											  ELSE
											  	   DELETED.cont3ct
									     END
									    ),			
			CEV.cont4ct = CEV.cont4ct - (CASE WHEN CEV.cont4chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont4chkval)
											  ELSE
											  	   DELETED.cont4ct
									     END
									    ),			
			CEV.cont5ct = CEV.cont5ct - (CASE WHEN CEV.cont5chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont5chkval)
											  ELSE
											  	   DELETED.cont5ct
									     END
									    ),			
			CEV.cont6ct = CEV.cont6ct - (CASE WHEN CEV.cont6chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont6chkval)
											  ELSE
											  	   DELETED.cont6ct
									     END
									    ),			
			CEV.cont7ct = CEV.cont7ct - (CASE WHEN CEV.cont7chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont7chkval)
											  ELSE
											  	   DELETED.cont7ct
									     END
									    ),			
			CEV.cont8ct = CEV.cont8ct - (CASE WHEN CEV.cont8chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont8chkval)
											  ELSE
											  	   DELETED.cont8ct
									     END
									    ),			
			CEV.cont9ct = CEV.cont9ct - (CASE WHEN CEV.cont9chk = 1 THEN
												   CONVERT(NUMERIC(10,0),DELETED.cont9chkval)
											  ELSE
											  	   DELETED.cont9ct
									     END
									    )			
		FROM DELETED
		WHERE CEV.cevstamp = DELETED.cevstamp
		
		-- Segundo Paço
		UPDATE CEV
		SET	CEV.cont1ct = CEV.cont1ct + (CASE WHEN CEV.cont1chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont1chkval)
											  ELSE
												   INSERTED.cont1ct
									     END
									    ),
			CEV.cont2ct = CEV.cont2ct + (CASE WHEN CEV.cont2chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont2chkval)
											  ELSE
											  	   INSERTED.cont2ct
									     END
									    ),			
			CEV.cont3ct = CEV.cont3ct + (CASE WHEN CEV.cont3chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont3chkval)
											  ELSE
											  	   INSERTED.cont3ct
									     END
									    ),			
			CEV.cont4ct = CEV.cont4ct + (CASE WHEN CEV.cont4chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont4chkval)
											  ELSE
											  	   INSERTED.cont4ct
									     END
									    ),			
			CEV.cont5ct = CEV.cont5ct + (CASE WHEN CEV.cont5chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont5chkval)
											  ELSE
											  	   INSERTED.cont5ct
									     END
									    ),			
			CEV.cont6ct = CEV.cont6ct + (CASE WHEN CEV.cont6chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont6chkval)
											  ELSE
											  	   INSERTED.cont6ct
									     END
									    ),			
			CEV.cont7ct = CEV.cont7ct + (CASE WHEN CEV.cont7chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont7chkval)
											  ELSE
											  	   INSERTED.cont7ct
									     END
									    ),			
			CEV.cont8ct = CEV.cont8ct + (CASE WHEN CEV.cont8chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont8chkval)
											  ELSE
											  	   INSERTED.cont8ct
									     END
									    ),			
			CEV.cont9ct = CEV.cont9ct + (CASE WHEN CEV.cont9chk = 1 THEN
												   CONVERT(NUMERIC(10,0),INSERTED.cont9chkval)
											  ELSE
											  	   INSERTED.cont9ct
									     END
									    )
		FROM INSERTED			
		WHERE CEV.cevstamp = INSERTED.cevstamp
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_fi_update ON fi WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONDECLARE @Cinfofi VARBINARY(128) SELECT @Cinfofi = Context_Info() IF @Cinfofi = 0x88888 BEGIN if update(usrinis) or update(usrhora) or update(usrdata) or update(fifref) or update(design) or update(ref) or update(oref) or update(qtt) or update(iectin) or update(vvenda) or update(tiliquido) or update(ivaincl) or update(iva) or update(num1) or update(etiliquido) or update(eiectin) or update(evvenda) or update(vbase ) or update(pv) or update(evbase) or update(epv) or update(usalote) or update(lote) or update(texteis) or update(cor) or update(tam) or update(tabiva) or update(ecomissao) or update(extracom) or update(eextracom) or update(fivendedor) or update(fivendnm) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(familia) or update(custo) or update(ecusto) or update(ftstamp ) or update(ndoc) or update(composto) or update(fistamp) or update(fno) or update(ficcusto) BEGINUPDATE PN SET pn.usrinis=inserted.usrinis,pn.usrhora=inserted.usrhora,pn.usrdata=inserted.usrdata,pn.classe=ft.classe,pn.fref=case when td.lifref=1 then inserted.fifref else ft.fref end,pn.ccusto=case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,pn.ncusto=case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,pn.nome=ft.nome,pn.segmento=ft.segmento,pn.fdata=ft.fdata,pn.design=inserted.design,pn.ref=case when inserted.ref<>'' then inserted.ref else inserted.oref end,pn.qtt=(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),pn.tiliquido=case when inserted.iectin<>0 then ROUND(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 1) end,pn.num1=inserted.num1,pn.pv=case when inserted.iectin<>0 then ROUND(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) end,pn.etiliquido=case when inserted.eiectin<>0 then ROUND(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 2) end,pn.epv=case when inserted.eiectin<>0 then ROUND(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) end,pn.valbase=ROUND((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),pn.evalbase=ROUND((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),pn.valuni=ROUND(case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),pn.evaluni=ROUND(case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),pn.texteis=inserted.texteis,pn.cor=inserted.cor,pn.tam=inserted.tam,pn.usalote=inserted.usalote,pn.lote=inserted.lote,pn.no=ft.no,pn.estab=ft.estab,pn.fno=ft.fno,pn.iva=inserted.iva,pn.tabiva=inserted.tabiva,pn.ecomissao=inserted.ecomissao,pn.extracom=inserted.extracom,pn.eextracom=inserted.eextracom,pn.nmdoc=ft.nmdoc,pn.vendedor=(case when td.LIVENDEDOR=1 then inserted.fivendedor else ft.vendedor end),pn.vendnm=(case when td.LIVENDEDOR=1 then inserted.fivendnm else ft.vendnm end),pn.zona=ft.zona,pn.tipo=ft.tipo,pn.usr1=inserted.usr1,pn.usr2=inserted.usr2,pn.usr3=inserted.usr3,pn.usr4=inserted.usr4,pn.usr5=inserted.usr5,pn.familia=inserted.familia,pn.pcusto=inserted.custo,pn.tcusto=(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*inserted.custo,pn.epcusto=inserted.ecusto,pn.etcusto=(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*inserted.ecusto,pn.cpoc=inserted.cpoc, pn.pctdesc=inserted.desconto, pn.area=ft2.area ,pn.snstamp=inserted.snstampfrom inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.coms=1 and inserted.qtt<>0 and inserted.composto=0 and inserted.fistamp=pn.fistampENDif update(qtt) BEGINDELETE from PN from inserted inner join td on inserted.ndoc=td.ndoc where td.coms=1 and inserted.qtt=0 and inserted.composto=0 and inserted.fistamp=pn.fistampENDif update(fistamp) or update(ftstamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrinis) or update(ousrhora) or update(ousrdata) or update(fifref) or update(design) or update(ref) or update(oref) or update(qtt) or update(iectin) or update(vvenda) or update(tiliquido) or update(ivaincl) or update(iva) or update(eiectin) or update(evvenda) or update(etiliquido) or update(vbase) or update(pv) or update(evbase) or update(epv) or update(num1) or update(texteis) or update(cor) or update(tam) or update(usalote) or update(lote) or update(tabiva) or update(ecomissao) or update(extracom) or update(eextracom) or update(fivendedor) or update(fivendnm) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(familia) or update(custo) or update(ecusto) or update(cpoc ) or update(ftstamp) or update(ndoc) or update(composto) or update(fistamp) or update(fno) or update(ficcusto) BEGININSERT INTO PN (classe,snstamp,pnstamp,fistamp,ftstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,segmento,fdata,design,ref,qtt,tiliquido,pv,etiliquido,epv,valbase,evalbase,valuni,evaluni,num1,texteis,cor,tam,usalote,lote,no,estab,fno,iva,tabiva,ecomissao,extracom,eextracom,nmdoc,vendedor,vendnm,zona,tipo,pago,pdata,usr1,usr2,usr3,usr4,usr5,familia,pcusto,tcusto,epcusto,etcusto,cpoc,pctdesc,area) select ft.classe, case when inserted.mhstamp <> SPACE(25) then '' when ft.snstamp = '' then inserted.snstamp  else ft.snstamp end ,inserted.fistamp,inserted.fistamp,inserted.ftstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',case when td.lifref=1 then inserted.fifref else ft.fref end,case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,ft.nome,ft.segmento,ft.fdata,inserted.design,case when inserted.ref<>'' then inserted.ref else inserted.oref end,(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),case when inserted.iectin<>0 then round(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 1) end,case when inserted.iectin<>0 then round(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) end,case when inserted.eiectin<>0 then round(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 2) end,case when inserted.eiectin<>0 then round(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) end,round((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),round((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),round(case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),round(case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),inserted.num1,inserted.texteis,inserted.cor,inserted.tam,inserted.usalote,inserted.lote,ft.no,ft.estab,ft.fno,inserted.iva,inserted.tabiva,inserted.ecomissao,inserted.extracom,inserted.eextracom,ft.nmdoc,case when td.LIVENDEDOR=1 then inserted.fivendedor else ft.vendedor end,case when td.LIVENDEDOR=1 then inserted.fivendnm else ft.vendnm end,ft.zona,ft.tipo,case when td.lancacc=1 and not td.ccautoreg=1 then 0 else 1 end,case when td.lancacc=1 and not td.ccautoreg=1 then  convert(datetime,'19000101') else ft.fdata end,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.familia,inserted.custo,inserted.custo*(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),inserted.ecusto,inserted.ecusto*(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),inserted.cpoc,inserted.desconto,ft2.area from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where ft.anulado = 0 and not EXISTS(select pnstamp from pn where pn.pnstamp=inserted.fistamp) and td.coms=1 and inserted.qtt<>0 and inserted.composto=0ENDDELETE FROM SV FROM inserted where sv.fistamp=inserted.fistamp and inserted.fistamp<>'' and inserted.ref=''DELETE FROM SV FROM inserted where sv.fistamp=inserted.fistamp and inserted.fistamp<>'' and inserted.epcp=inserted.ecusto and inserted.pcp=inserted.custoif (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo))>0 if update(usrinis) or update(usrhora) or update(usrdata) or update(design) or update(ref) or update(lote) or update(cor) or update(tam) or update(armazem) or update(qtt) or update(custo) or update(ecusto) or update(pcp) or update(epcp) or update(ftstamp) or update(ndoc) or update(fistamp)  or update(fno)update SV set usrdata=inserted.usrdata,usrhora=inserted.usrhora,usrinis=inserted.usrinis,nome=ft.nome,data=ft.fdata,design=inserted.design,ref=inserted.ref,lote=inserted.lote,cor=inserted.cor,tam=inserted.tam,armazem=inserted.armazem,tt=inserted.qtt*(inserted.custo-inserted.pcp),ett=inserted.qtt*(inserted.ecusto-inserted.epcp),no=ft.no from inserted inner join ft on inserted.ftstamp=ft.ftstamp inner join td on td.ndoc=inserted.ndoc where ft.anulado=0 and inserted.fistamp=sv.fistamp and td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo) and inserted.fistamp IN (select deleted.fistamp from deleted) if (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where not EXISTS(select svstamp from sv where sv.svstamp=inserted.fistamp) and td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo))>0 INSERT INTO SV (svstamp,fistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fistamp,inserted.fistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',ft.nome,'PTE ou EURO',ft.fdata,inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem,inserted.qtt*(inserted.custo-inserted.pcp),inserted.qtt*(inserted.ecusto-inserted.epcp),ft.no,convert(char (10), ft.fno),td.cmsv,td.cmsvn from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on ft.ndoc=td.ndoc where ft.anulado= 0 and not EXISTS(select svstamp from sv where sv.svstamp=inserted.fistamp) and td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo) RETURN END DELETE FROM SL FROM inserted where sl.fistamp=inserted.fistamp and inserted.fistamp<>'' and inserted.ref=''if update(usrdata) or update(usrhora) or update(usrinis) or update(fifref) or update(codigo) or update(design) or update(ref) or update(lote) or update(armazem) or update(qtt) or update(iectin) or update(vvenda) or update(ficcusto) or update(fincusto) or update(desconto) or update(desc2) or update(desc3) or update(desc4) or update(desc5) or update(desc6) or update(pv) or update(iva) or update(ivaincl) or update(eiectin) or update(evvenda) or update(num1) or update(composto) or update(cor) or update(tam) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(iectmtot) or update(mvbase ) or update(pvmoeda) or update(mvvenda ) or update(custo) or update(pcp) or update(cpoc) or update(vbase ) or update(tiliquido) or update(epv) or update(evbase) or update(etiliquido) or update(ecusto) or update(epcp) or update(ftstamp) or update(ndoc) or update(fistamp) or update(fno) BEGINUPDATE SL set sl.usrdata=inserted.usrdata,sl.usrhora=inserted.usrhora,sl.usrinis=inserted.usrinis,sl.fref=case when td.lifref=1 then inserted.fifref else ft.fref end,sl.ccusto=case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,sl.ncusto=case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,sl.nome=ft.nome,sl.segmento=ft.segmento,sl.codigo=inserted.codigo,sl.moeda=ft.moeda,sl.datalc=ft.fdata,sl.design=inserted.design,sl.ref=inserted.ref,sl.lote=inserted.lote,sl.armazem=inserted.armazem,sl.qtt=case when ft.tipodoc=3 then inserted.qtt*-1 else inserted.qtt end,sl.tt=inserted.sltt,sl.vu=inserted.slvu,sl.ett=inserted.esltt,sl.evu=inserted.eslvu,sl.num1=inserted.num1,sl.frcl=ft.no,sl.adoc=convert(char (10), ft.fno),sl.cm=td.cmsl,sl.cmdesc=td.cmsln,sl.composto=inserted.composto,sl.cor=inserted.cor,sl.tam=inserted.tam,sl.usr1=inserted.usr1,sl.usr2=inserted.usr2,sl.usr3=inserted.usr3,sl.usr4=inserted.usr4,sl.usr5=inserted.usr5,sl.vumoeda=inserted.slvumoeda,sl.ttmoeda=inserted.slttmoeda,sl.pcpond=inserted.pcp,sl.cpoc=inserted.cpoc,sl.valiva=case when inserted.iectin<>0 then inserted.pv-inserted.vbase else case when inserted.ivaincl=1 then inserted.tiliquido-inserted.tiliquido/(1+inserted.iva/100) else inserted.tiliquido*inserted.iva/100 end end,sl.evaliva=case when inserted.eiectin<>0 then inserted.epv-inserted.evbase else case when inserted.ivaincl=1 then inserted.etiliquido-inserted.etiliquido/(1+inserted.iva/100) else inserted.etiliquido*inserted.iva/100 end end,sl.epcpond=inserted.epcp, sl.stns=inserted.stns from inserted inner join ft on inserted.ftstamp=ft.ftstamp inner join td on td.ndoc=inserted.ndoc where inserted.fistamp=sl.fistamp and td.lancasl=1 and inserted.fistamp IN (select deleted.fistamp from deleted) ENDif update(fistamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrinis) or update(ousrhora) or update(ousrdata) or update(fifref) or update(codigo) or update(design) or update(ref) or update(lote) or update(armazem) or update(qtt) or update(iectin) or update(vvenda) or update(desconto) or update(desc2) or update(desc3) or update(desc4) or update(desc5) or update(desc6) or update(pv) or update(iva) or update(ivaincl) or update(eiectin) or update(evvenda) or update(epv) or update(num1) or update(composto) or update(cor) or update(tam) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(iectmtot) or update(mvbase) or update(pvmoeda) or update(mvvenda ) or update(custo) or update(pcp) or update(cpoc) or update(vbase ) or update(tiliquido) or update(evbase) or update(etiliquido) or update(ecusto ) or update(epcp) or update(ftstamp) or update(ndoc) or update(fistamp) or update(fno) BEGININSERT INTO SL (slstamp,fistamp,stns,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,segmento,codigo,moeda,datalc,design,ref,lote,armazem,qtt,tt,vu,ett,evu,num1,frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,vumoeda,ttmoeda,pcpond,cpoc,valiva,evaliva,epcpond )select inserted.fistamp,inserted.fistamp,inserted.stns,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',case when td.lifref=1 then inserted.fifref else ft.fref end,case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,ft.nome,ft.segmento,inserted.codigo,ft.moeda,ft.fdata,inserted.design,inserted.ref,inserted.lote,inserted.armazem,case when ft.tipodoc=3 then inserted.qtt*-1 else inserted.qtt end,inserted.sltt,inserted.slvu,inserted.esltt,inserted.eslvu,inserted.num1,ft.no,convert(char (10), ft.fno),td.cmsl,td.cmsln,inserted.composto,inserted.cor,inserted.tam,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.slvumoeda,inserted.slttmoeda,inserted.pcp,inserted.cpoc,case when inserted.iectin<>0 then inserted.pv-inserted.vbase else case when inserted.ivaincl=1 then inserted.tiliquido-inserted.tiliquido/(1+inserted.iva/100) else inserted.tiliquido*inserted.iva/100 end end,case when inserted.eiectin<>0 then inserted.epv-inserted.evbase else case when inserted.ivaincl=1 then inserted.etiliquido-inserted.etiliquido/(1+inserted.iva/100) else inserted.etiliquido*inserted.iva/100 end end,inserted.epcp from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where ft.anulado=0 and not EXISTS(select slstamp from sl where sl.slstamp=inserted.fistamp) and td.lancasl=1 and inserted.ref<>space(18) ENDDELETE FROM SV FROM inserted where sv.fistamp=inserted.fistamp and inserted.fistamp<>'' and inserted.ref=''DELETE FROM SV FROM inserted where sv.fistamp=inserted.fistamp and inserted.fistamp<>'' and inserted.epcp=inserted.ecusto and inserted.pcp=inserted.custoif (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo))>0 if update(usrinis) or update(usrhora) or update(usrdata) or update(design) or update(ref) or update(lote) or update(cor) or update(tam) or update(armazem) or update(qtt) or update(custo) or update(ecusto) or update(pcp) or update(epcp) or update(ftstamp) or update(ndoc) or update(fistamp)  or update(fno)update SV set usrdata=inserted.usrdata,usrhora=inserted.usrhora,usrinis=inserted.usrinis,nome=ft.nome,data=ft.fdata,design=inserted.design,ref=inserted.ref,lote=inserted.lote,cor=inserted.cor,tam=inserted.tam,armazem=inserted.armazem,tt=inserted.qtt*(inserted.custo-inserted.pcp),ett=inserted.qtt*(inserted.ecusto-inserted.epcp),no=ft.no from inserted inner join ft on inserted.ftstamp=ft.ftstamp inner join td on td.ndoc=inserted.ndoc where ft.anulado=0 and inserted.fistamp=sv.fistamp and td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo) and inserted.fistamp IN (select deleted.fistamp from deleted) if (select count(*) from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where not EXISTS(select svstamp from sv where sv.svstamp=inserted.fistamp) and td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo))>0 INSERT INTO SV (svstamp,fistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,nome,moeda,data,design,ref,lote,cor,tam,armazem,tt,ett,no,adoc,cm,cmdesc)select inserted.fistamp,inserted.fistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',ft.nome,'PTE ou EURO',ft.fdata,inserted.design,inserted.ref,inserted.lote,inserted.cor,inserted.tam,inserted.armazem,inserted.qtt*(inserted.custo-inserted.pcp),inserted.qtt*(inserted.ecusto-inserted.epcp),ft.no,convert(char (10), ft.fno),td.cmsv,td.cmsvn from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join td on ft.ndoc=td.ndoc where ft.anulado= 0 and not EXISTS(select svstamp from sv where sv.svstamp=inserted.fistamp) and td.lancasl=1 and td.cmsl>50 and td.cmsv<>0 and td.tipodoc=3 and inserted.qtt<>0 and inserted.ref<>space(18) and (inserted.epcp<>inserted.ecusto or inserted.pcp<>inserted.custo) UPDATE FTREG SET  ftreg.vciva=ftreg.vciva-  (select ROUND(sum(case when deleted.ivaincl=1 then ((deleted.tiliquido)*-1)*(1-ftreg.fin/100) else -((deleted.tiliquido-round((deleted.tiliquido*ftreg.fin/100),0))*(1+deleted.iva/100)) end),0)  from deleted, ftreg where deleted.ftregstamp=ftreg.ftregstamp),   ftreg.vmoeda=ftreg.vmoeda-  (select ROUND(sum(case when deleted.ivaincl=1 then ((deleted.tmoeda)*-1)*(1-ftreg.fin/100) else -((deleted.tmoeda-round((deleted.tmoeda*ftreg.fin/100),2))*(1+deleted.iva/100)) end),2)   from deleted, ftreg where deleted.ftregstamp=ftreg.ftregstamp),   ftreg.evciva=ftreg.evciva-  (select ROUND(sum(case when deleted.ivaincl=1 then ((deleted.etiliquido)*-1)*(1-ftreg.fin/100) else -((deleted.etiliquido-round((deleted.etiliquido*ftreg.fin/100),2))*(1+deleted.iva/100)) end),2)  from deleted, ftreg where deleted.ftregstamp=ftreg.ftregstamp),  ftreg.ultdoc='',  ftreg.fin = isnull((select top 1 ft.fin from ft left join deleted on deleted.ftstamp=ft.ftstamp  where deleted.ftregstamp=ftreg.ftregstamp),0) from ftreg inner join deleted on ftreg.ftregstamp=deleted.ftregstamp where deleted.tipodoc=3 and deleted.ftregstamp<>space(25) if update(ftregstamp) BEGININSERT INTO FTREG (ftregstamp)select distinct inserted.ftregstamp from inserted where inserted.ftregstamp<>space(25) and not EXISTS(select ftregstamp from ftreg where ftreg.ftregstamp=inserted.ftregstamp) ENDUPDATE FTREG SET  ftreg.vciva=ftreg.vciva+  (select ROUND(sum(case when inserted.ivaincl=1 then ((inserted.tiliquido)*-1)*(1-ft.fin/100) else -((inserted.tiliquido-round((inserted.tiliquido*ft.fin/100),0))*(1+inserted.iva/100)) end),0)  from inserted, ft where inserted.ftregstamp=ftreg.ftregstamp and inserted.ftstamp = ft.ftstamp),   ftreg.vmoeda=ftreg.vmoeda+  (select ROUND(sum(case when inserted.ivaincl=1 then ((inserted.tmoeda)*-1)*(1-ft.fin/100) else -((inserted.tmoeda-round((inserted.tmoeda*ft.fin/100),2))*(1+inserted.iva/100)) end),2)   from inserted, ft where inserted.ftregstamp=ftreg.ftregstamp and inserted.ftstamp = ft.ftstamp),   ftreg.evciva=ftreg.evciva+  (select ROUND(sum(case when inserted.ivaincl=1 then ((inserted.etiliquido)*-1)*(1-ft.fin/100) else -((inserted.etiliquido-round((inserted.etiliquido*ft.fin/100),2))*(1+inserted.iva/100)) end),2)   from inserted, ft where inserted.ftregstamp=ftreg.ftregstamp and inserted.ftstamp = ft.ftstamp),  ftreg.ultdoc=(select ltrim(rtrim(ft.nmdoc))+' nº '+convert(char(10),ft.fno)+' de '+convert(char(10),ft.fdata,104) from ft where inserted.ftstamp=ft.ftstamp),  ftreg.fin = isnull((select top 1 ft.fin from ft left join inserted on inserted.ftstamp=ft.ftstamp  where inserted.ftregstamp=ftreg.ftregstamp),0) from ftreg inner join inserted on ftreg.ftregstamp=inserted.ftregstamp where inserted.tipodoc=3 and inserted.ftregstamp<>space(25)if update(qtt) or update(ftstamp) or update(bistamp) BEGINUPDATE BI SET bi.fno = 0,bi.adoc = '',bi.nmdoc = '',bi.oftstamp = '',bi.ndoc = 0, bi.qtt2 = bi.qtt2-isnull((select SUM(deleted.qtt *(case when deleted.tipodoc=3 then -1 else 1 end)) from deleted inner join fi2 on fi2.fi2stamp=deleted.fistamp where deleted.bistamp <> '' and deleted.bistamp = bi.bistamp and fi2.noupdqtt2ori=0),0) from bi inner join deleted on deleted.bistamp=bi.bistamp where deleted.bistamp<>'' UPDATE BI SET bi.partes2 = bi.partes2-(select SUM(deleted.partes) from deleted where deleted.bistamp <> '' and deleted.bistamp = bi.bistamp) from bi inner join deleted on deleted.bistamp = bi.bistamp inner join ts (nolock) on ts.ndos = bi.ndos where deleted.bistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1)UPDATE BI SET bi.fdata = ft.fdata,bi.fno = ft.fno,bi.adoc = convert(char(10),ft.fno),bi.oftstamp = ft.ftstamp,bi.nmdoc = ft.nmdoc,bi.ndoc = ft.ndoc, bi.qtt2 = bi.qtt2+isnull((select SUM(inserted.qtt*(case when inserted.tipodoc=3 then -1 else 1 end)) from inserted inner join fi2 on fi2.fi2stamp=inserted.fistamp where inserted.bistamp <> '' and inserted.bistamp = bi.bistamp and fi2.noupdqtt2ori = 0),0) from bi inner join inserted on inserted.bistamp=bi.bistamp inner join ft (nolock) on inserted.ftstamp=ft.ftstamp where inserted.bistamp<>'' UPDATE BI SET bi.partes2 = bi.partes2+(select SUM(inserted.partes*(case when inserted.tipodoc=3 then -1 else 1 end)) from inserted where inserted.bistamp <> '' and inserted.bistamp = bi.bistamp) from bi inner join inserted on inserted.bistamp = bi.bistamp inner join ts (nolock) on ts.ndos = bi.ndos where inserted.bistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1)ENDif (select count(*) from inserted where ndocft<>0 and fnoft<>0 ) >0 UPDATE FT SET ft.facturada = 1 ,ft.nmdocft = td.nmdoc, ft.fnoft = inserted.fno  from inserted inner join td on td.ndoc=inserted.ndoc  inner join ft on ft.ftstamp=inserted.ftstamp  where inserted.ndocft<>0 and inserted.ndocft = ft.ndoc and inserted.fnoft = ft.fno and inserted.ftanoft = ft.ftano and ft.anulado=0 if update(usrinis) or update(usrhora) or update(usrdata) or update(fifref) or update(design) or update(ref) or update(oref) or update(qtt) or update(iectin) or update(vvenda) or update(tiliquido) or update(ivaincl) or update(iva) or update(num1) or update(etiliquido) or update(eiectin) or update(evvenda) or update(vbase ) or update(pv) or update(evbase) or update(epv) or update(usalote) or update(lote) or update(texteis) or update(cor) or update(tam) or update(tabiva) or update(ecomissao) or update(extracom) or update(eextracom) or update(fivendedor) or update(fivendnm) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(familia) or update(custo) or update(ecusto) or update(ftstamp ) or update(ndoc) or update(composto) or update(fistamp) or update(fno) or update(ficcusto) BEGINUPDATE PN SET pn.usrinis=inserted.usrinis,pn.usrhora=inserted.usrhora,pn.usrdata=inserted.usrdata,pn.classe=ft.classe,pn.fref=case when td.lifref=1 then inserted.fifref else ft.fref end,pn.ccusto=case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,pn.ncusto=case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,pn.nome=ft.nome,pn.segmento=ft.segmento,pn.fdata=ft.fdata,pn.design=inserted.design,pn.ref=case when inserted.ref<>'' then inserted.ref else inserted.oref end,pn.qtt=(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),pn.tiliquido=case when inserted.iectin<>0 then ROUND(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 1) end,pn.num1=inserted.num1,pn.pv=case when inserted.iectin<>0 then ROUND(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) end,pn.etiliquido=case when inserted.eiectin<>0 then ROUND(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 2) end,pn.epv=case when inserted.eiectin<>0 then ROUND(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) end,pn.valbase=ROUND((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),pn.evalbase=ROUND((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),pn.valuni=ROUND(case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),pn.evaluni=ROUND(case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),pn.texteis=inserted.texteis,pn.cor=inserted.cor,pn.tam=inserted.tam,pn.usalote=inserted.usalote,pn.lote=inserted.lote,pn.no=ft.no,pn.estab=ft.estab,pn.fno=ft.fno,pn.iva=inserted.iva,pn.tabiva=inserted.tabiva,pn.ecomissao=inserted.ecomissao,pn.extracom=inserted.extracom,pn.eextracom=inserted.eextracom,pn.nmdoc=ft.nmdoc,pn.vendedor=(case when td.LIVENDEDOR=1 then inserted.fivendedor else ft.vendedor end),pn.vendnm=(case when td.LIVENDEDOR=1 then inserted.fivendnm else ft.vendnm end),pn.zona=ft.zona,pn.tipo=ft.tipo,pn.usr1=inserted.usr1,pn.usr2=inserted.usr2,pn.usr3=inserted.usr3,pn.usr4=inserted.usr4,pn.usr5=inserted.usr5,pn.familia=inserted.familia,pn.pcusto=inserted.custo,pn.tcusto=(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*inserted.custo,pn.epcusto=inserted.ecusto,pn.etcusto=(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*inserted.ecusto,pn.cpoc=inserted.cpoc, pn.pctdesc=inserted.desconto, pn.area=ft2.area ,pn.snstamp=inserted.snstampfrom inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where td.coms=1 and inserted.qtt<>0 and inserted.composto=0 and inserted.fistamp=pn.fistampENDif update(qtt) BEGINDELETE from PN from inserted inner join td on inserted.ndoc=td.ndoc where td.coms=1 and inserted.qtt=0 and inserted.composto=0 and inserted.fistamp=pn.fistampENDif update(fistamp) or update(ftstamp) or update(usrinis) or update(usrhora) or update(usrdata) or update(ousrinis) or update(ousrhora) or update(ousrdata) or update(fifref) or update(design) or update(ref) or update(oref) or update(qtt) or update(iectin) or update(vvenda) or update(tiliquido) or update(ivaincl) or update(iva) or update(eiectin) or update(evvenda) or update(etiliquido) or update(vbase) or update(pv) or update(evbase) or update(epv) or update(num1) or update(texteis) or update(cor) or update(tam) or update(usalote) or update(lote) or update(tabiva) or update(ecomissao) or update(extracom) or update(eextracom) or update(fivendedor) or update(fivendnm) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(familia) or update(custo) or update(ecusto) or update(cpoc ) or update(ftstamp) or update(ndoc) or update(composto) or update(fistamp) or update(fno) or update(ficcusto) BEGININSERT INTO PN (classe,snstamp,pnstamp,fistamp,ftstamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,segmento,fdata,design,ref,qtt,tiliquido,pv,etiliquido,epv,valbase,evalbase,valuni,evaluni,num1,texteis,cor,tam,usalote,lote,no,estab,fno,iva,tabiva,ecomissao,extracom,eextracom,nmdoc,vendedor,vendnm,zona,tipo,pago,pdata,usr1,usr2,usr3,usr4,usr5,familia,pcusto,tcusto,epcusto,etcusto,cpoc,pctdesc,area) select ft.classe, case when inserted.mhstamp <> SPACE(25) then '' when ft.snstamp = '' then inserted.snstamp  else ft.snstamp end ,inserted.fistamp,inserted.fistamp,inserted.ftstamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'FT',case when td.lifref=1 then inserted.fifref else ft.fref end,case when td.liccusto=1 then inserted.ficcusto else ft.ccusto end,case when td.lincusto=1 then inserted.fincusto else ft.ncusto end,ft.nome,ft.segmento,ft.fdata,inserted.design,case when inserted.ref<>'' then inserted.ref else inserted.oref end,(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),case when inserted.iectin<>0 then round(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 1) end,case when inserted.iectin<>0 then round(inserted.vvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) else round(inserted.tiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 1) end,case when inserted.eiectin<>0 then round(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end), 2) end,case when inserted.eiectin<>0 then round(inserted.evvenda*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) else round(inserted.etiliquido*(case when inserted.nsujpp=1 then 1 else 1-ft.fin/100 end)/(case when inserted.ivaincl=1 then 1+inserted.iva/100 else 1 end)/(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end), 2) end,round((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),round((case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end)*case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),round(case when inserted.iectin<>0 then inserted.vbase else inserted.pv end, 1),round(case when inserted.eiectin<>0 then inserted.evbase else inserted.epv end, 2),inserted.num1,inserted.texteis,inserted.cor,inserted.tam,inserted.usalote,inserted.lote,ft.no,ft.estab,ft.fno,inserted.iva,inserted.tabiva,inserted.ecomissao,inserted.extracom,inserted.eextracom,ft.nmdoc,case when td.LIVENDEDOR=1 then inserted.fivendedor else ft.vendedor end,case when td.LIVENDEDOR=1 then inserted.fivendnm else ft.vendnm end,ft.zona,ft.tipo,case when td.lancacc=1 and not td.ccautoreg=1 then 0 else 1 end,case when td.lancacc=1 and not td.ccautoreg=1 then  convert(datetime,'19000101') else ft.fdata end,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.familia,inserted.custo,inserted.custo*(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),inserted.ecusto,inserted.ecusto*(case when td.tipodoc=3 then -inserted.qtt else inserted.qtt end),inserted.cpoc,inserted.desconto,ft2.area from inserted inner join ft on ft.ftstamp=inserted.ftstamp inner join ft2 on ft2.ft2stamp=inserted.ftstamp inner join td on inserted.ndoc=td.ndoc where ft.anulado = 0 and not EXISTS(select pnstamp from pn where pn.pnstamp=inserted.fistamp) and td.coms=1 and inserted.qtt<>0 and inserted.composto=0ENDIF UPDATE(QTT) OR UPDATE(CUSTO) OR UPDATE(ECUSTO)BEGINUPDATE FT SET  ft.custo=ft.custo-(select case when deleted.tipodoc=3 then sum(-deleted.qtt*deleted.custo) else sum(deleted.qtt*deleted.custo) end  from deleted where deleted.ftstamp=ft.ftstamp and deleted.composto=0 group by deleted.tipodoc) ,  ft.ecusto=ft.ecusto-(select case when deleted.tipodoc=3 then sum(-deleted.qtt*deleted.ecusto) else sum(deleted.qtt*deleted.ecusto) end  from deleted where deleted.ftstamp=ft.ftstamp and deleted.composto=0 group by deleted.tipodoc) from deleted where deleted.ftstamp=ft.ftstamp and deleted.composto=0 UPDATE FT SET  ft.custo=ft.custo+(select case when inserted.tipodoc=3 then sum(-inserted.qtt*inserted.custo) else sum(inserted.qtt*inserted.custo) end  from inserted where inserted.ftstamp=ft.ftstamp and inserted.composto=0 and ft.anulado = 0 group by inserted.tipodoc) ,  ft.ecusto=ft.ecusto+(select case when inserted.tipodoc=3 then sum(-inserted.qtt*inserted.ecusto) else sum(inserted.qtt*inserted.ecusto) end  from inserted where inserted.ftstamp=ft.ftstamp and inserted.composto=0 and ft.anulado = 0 group by inserted.tipodoc) from inserted where inserted.ftstamp=ft.ftstamp and inserted.composto=0 and ft.anulado = 0 ENDGO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_PD_insert ON PD WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO FC (fcstamp,pdstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
datalc,no,nome,cm,cmdesc,origem,deb,edeb,
debm, virs, evirs,
eivacativado, ivacativado, mivacativado,
ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,
eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,
formapag,situacao,
moeda,adoc,dataven,fref,intid,ccusto,ncusto,zona,tipo,pais,estab,obs, cambiofixo,crend)
select inserted.pdstamp,inserted.pdstamp,
inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,
inserted.rdata,inserted.no,inserted.nome,inserted.cm,inserted.cmdesc,'PD',
inserted.total, inserted.etotal, inserted.totalmoeda,
inserted.virs, inserted.evirs,
inserted.eivacativado, inserted.ivacativado, inserted.mivacativado,
case when inserted.tabiva=1 then inserted.ivav else 0 end,
case when inserted.tabiva=2 then inserted.ivav else 0 end,
case when inserted.tabiva=3 then inserted.ivav else 0 end,
case when inserted.tabiva=4 then inserted.ivav else 0 end,
case when inserted.tabiva=5 then inserted.ivav else 0 end,
case when inserted.tabiva=6 then inserted.ivav else 0 end,
case when inserted.tabiva=7 then inserted.ivav else 0 end,
case when inserted.tabiva=8 then inserted.ivav else 0 end,
case when inserted.tabiva=9 then inserted.ivav else 0 end,
case when inserted.tabiva=1 then inserted.eivav else 0 end,
case when inserted.tabiva=2 then inserted.eivav else 0 end,
case when inserted.tabiva=3 then inserted.eivav else 0 end,
case when inserted.tabiva=4 then inserted.eivav else 0 end,
case when inserted.tabiva=5 then inserted.eivav else 0 end,
case when inserted.tabiva=6 then inserted.eivav else 0 end,
case when inserted.tabiva=7 then inserted.eivav else 0 end,
case when inserted.tabiva=8 then inserted.eivav else 0 end,
case when inserted.tabiva=9 then inserted.eivav else 0 end,
inserted.formapag,'1',
inserted.moeda,inserted.adoc,inserted.rdata,inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.zona,inserted.tipo,inserted.pais,inserted.estab,
inserted.descricao, inserted.cambiofixo,inserted.crend
from inserted
INSERT INTO FC (fcstamp,pdstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,
formapag,situacao,
datalc,no,nome,cm,cmdesc,origem,
deb,edeb,debf,edebf,cred,ecred,credf,ecredf,
moeda,adoc,dataven,fref,ccusto,ncusto,intid,tipo,estab,pais,zona,obs, cambiofixo)
select 'I'+inserted.pdstamp,inserted.pdstamp,
inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,
inserted.formapag,'2',
inserted.rdata,inserted.no,inserted.nome,
0,
'Indefinido!',
'PD',
case when inserted.virs<0 then 0 else inserted.virs end,case when inserted.evirs<0 then 0 else inserted.evirs end,
case when inserted.virs<0 then 0 else inserted.virs end,case when inserted.evirs<0 then 0 else inserted.evirs end,
case when inserted.virs>0 then 0 else -inserted.virs end,case when inserted.evirs>0 then 0 else -inserted.evirs end,
case when inserted.virs>0 then 0 else -inserted.virs end,case when inserted.evirs>0 then 0 else -inserted.evirs end,
'PTE ou EURO',inserted.adoc,inserted.rdata,inserted.fref,inserted.ccusto,inserted.ncusto,inserted.intid,inserted.tipo,inserted.estab,inserted.pais,inserted.zona,
inserted.descricao, inserted.cambiofixo
from inserted where inserted.virs<>0 or inserted.evirs<>0
INSERT INTO OL (olstamp,pdstamp,
usrdata,usrhora,usrinis,
ousrdata,ousrhora,ousrinis,origem,
documento,cheque,
data,dvalor,said,esaid,saidm,moeda,descricao,olcodigo,grupo,sgrupo,fref,intid,ccusto,ncusto,ollocal,contado,local,operext,cecope)
select inserted.pdstamp,inserted.pdstamp,
inserted.usrdata,inserted.usrhora,inserted.usrinis,
inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,'PD',
inserted.cmdesc,inserted.adoc,
inserted.rdata,inserted.rdata,inserted.total - isnull(inserted.ivacativado, 0),
inserted.etotal - isnull(inserted.eivacativado, 0),inserted.valorm2,inserted.moeda2,
inserted.nome,inserted.olcodigo,oc.olgrupo,oc.olsgrupo,
inserted.fref,inserted.intid,inserted.ccusto,inserted.ncusto,inserted.ollocal,inserted.contado,inserted.telocal,inserted.operext,inserted.cecope
from inserted inner join oc on oc.olcodigo=inserted.olcodigo 

IF exists (select 1 from inserted where inserted.cheque <> '')				
UPDATE chf
SET deststamp = inserted.pdstamp
, destino = 'PD'
, nome = case when chf.nome = '' then inserted.nome else chf.nome end 
, no = case when chf.no = 0 then inserted.no else chf.no end 
, estab = case when chf.no = 0 then inserted.estab else chf.estab end 
from inserted inner join chf on inserted.chfstamp = chf.chfstamp 
where inserted.cheque <> '' 
GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_bi_update ON bi WITH ENCRYPTION FOR UPDATE  AS SET NOCOUNT ONif exists (select deleted.marcada from deleted inner join inserted on deleted.bistamp=inserted.bistamp inner join bo2 on inserted.bostamp = bo2.bo2stamp where (deleted.emconf=0 or inserted.emconf=0) and bo2.anulado=0) BEGIN if (select count(*) from inserted inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado=0)>0
BEGIN
IF ( select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on inserted.ndos=ts.ndos where ts.stocks=1 and inserted.ref<>space(18) and bo2.anulado=0)>0
INSERT INTO SL
(slstamp,bistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,
fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,
ref,armazem,qtt,
lote,
tt,
vu,
ett,
evu,
num1,
frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,
vumoeda,
ttmoeda,
pcpond,cpoc,
valiva,trfa,evaliva,epcpond,stns
)
select inserted.bistamp,inserted.bistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BO',
case when ts.lifref=1 then inserted.bifref else bo.fref end,case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,bo.nome,inserted.codigo,bo.moeda,inserted.rdata,inserted.design,
inserted.ref,inserted.armazem,inserted.qtt,
inserted.lote,
inserted.sltt,
inserted.slvu,
inserted.esltt,
inserted.eslvu,
inserted.num1,
bo.no,convert(char (10), bo.obrano),
case when (ts.producao=1 and inserted.producao=1) Or (ts2.trocaequi=1 and inserted.trocaequi=1) then ts.cm2stocks else ts.cmstocks end,
case when (ts.producao=1 and inserted.producao=1) Or (ts2.trocaequi=1 and inserted.trocaequi=1) then ts.cm2desc else ts.cmdesc end,
inserted.composto,inserted.cor,inserted.tam,
inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,
inserted.slvumoeda,
inserted.slttmoeda,
inserted.pcusto,inserted.cpoc,
case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end,ts.trfa,
case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,
inserted.epcusto,inserted.stns
from inserted
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
inner join bo on bo.bostamp=inserted.bostamp inner join ts on inserted.ndos=ts.ndos inner join ts2 on ts2.ts2stamp=ts.tsstamp
 inner join bo2 on bo2.bo2stamp=inserted.bostamp
where ts.stocks=1 and inserted.ref<>'' and inserted.emconf=0 and bo2.anulado=0
IF (select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on inserted.ndos=ts.ndos where ts.stocks=1 and ts.trfa=1 and inserted.ref<>'' and bo2.anulado=0 )>0
INSERT INTO SL
(slstamp,bistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,
fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,
ref,armazem,qtt,
lote,
tt,
vu,
ett,
evu,
num1,
frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,
vumoeda,
ttmoeda,
pcpond,cpoc,
valiva,trfa,evaliva,epcpond,stns
)
select substring('T'+inserted.bistamp,1,25),inserted.bistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BO',
case when ts.lifref=1 then inserted.bifref else bo.fref end,case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,bo.nome,inserted.codigo,bo.moeda,inserted.rdata,inserted.design,
inserted.ref,inserted.ar2mazem,inserted.qtt,
inserted.lote,
inserted.sltt,
inserted.slvu,
inserted.esltt,
inserted.eslvu,
inserted.num1,
bo.no,convert(char (10), bo.obrano),ts.cm2stocks,ts.cm2desc,inserted.composto,inserted.cor,inserted.tam,
inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,
inserted.slvumoeda,
inserted.slttmoeda,
inserted.pcusto,inserted.cpoc,
case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end,ts.trfa,
case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,
inserted.epcusto,inserted.stns
from inserted
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
 inner join bo2 on bo2.bo2stamp=inserted.bostamp
inner join bo on bo.bostamp=inserted.bostamp inner join ts on inserted.ndos=ts.ndos where ts.stocks=1 and ts.trfa=1  and inserted.ref<>space(18) and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttcli=st.qttcli+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref) i
where st.ref=i.ref
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttcat=st.qttcat+(case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end)
from st inner join inserted on st.ref=inserted.ref inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and st.ref=inserted.ref and inserted.cativo=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttfor=st.qttfor+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref) i
where st.ref=i.ref
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resusr=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.usrqtt=st.usrqtt+(case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end)
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and st.ref=inserted.ref and inserted.resusr=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE ST SET
st.qttrec=st.qttrec+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref) i
where st.ref=i.ref
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.fechada=0 and bo2.anulado=0 )>0
INSERT INTO SA (sastamp,armazem,ref) select convert(char(5),inserted.armazem)+inserted.ref,inserted.armazem,inserted.ref
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where not EXISTS(select sa.armazem from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.ref<>'' and inserted.emconf=0 and bo2.anulado=0
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.rescli=sa.rescli+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if (select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.rescat=sa.rescat+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.resfor=sa.resfor+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.resfor=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and bo2.anulado=0)>0
UPDATE SA SET
sa.qttrec=sa.qttrec+(case when i.valor>0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.resrec=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem) i
where sa.ref=i.ref and sa.armazem=i.armazem
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
INSERT INTO SX (sxstamp,armazem,ref,cor,tam,rescli ,resfor ,qttrec,ststamp) select substring(cast(newid() as char(36)),1,25),inserted.armazem,inserted.ref,inserted.cor,inserted.tam,0,0,0,st.ststamp
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join st on inserted.ref=st.ref
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.cor<>'' and inserted.tam<>''
and not EXISTS(select armazem from sx where sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam) and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1)
and inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.emconf=0 and bo2.anulado=0
GROUP BY inserted.armazem,inserted.ref,inserted.cor,inserted.tam,st.ststamp
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
UPDATE Sx SET
sx.rescli =sx.rescli +(case when i.valor >0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem, inserted.cor,inserted.tam
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.rescli=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem,inserted.cor,inserted.tam) i
where sx.ref=i.ref and sx.armazem=i.armazem and sx.cor = i.cor and sx.tam = i.tam
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
UPDATE Sx SET
sx.resfor =sx.resfor+(case when i.valor >0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem, inserted.cor,inserted.tam
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0
where inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.resfor=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0
group by inserted.ref,inserted.armazem,inserted.cor,inserted.tam) i
where sx.ref=i.ref and sx.armazem=i.armazem and sx.cor = i.cor and sx.tam = i.tam
if ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0
UPDATE Sx SET
sx.qttrec =sx.qttrec +(case when i.valor >0 then i.valor else 0 end)
from (select SUM(inserted.qtt-inserted.qtt2) as valor,inserted.ref,inserted.armazem, inserted.cor,inserted.tam
from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp 
inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 
where inserted.ref<>'' and inserted.cor<>'' and inserted.tam<>'' and inserted.resrec=1  and inserted.fechada=0 and inserted.emconf=0 and bo2.anulado=0 
group by inserted.ref,inserted.armazem,inserted.cor,inserted.tam) i
where sx.ref=i.ref and sx.armazem=i.armazem and sx.cor = i.cor and sx.tam = i.tamif ( select count(*) from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and (inserted.cor<>'' or inserted.tam<>'') and bo2.anulado=0)>0  UPDATE Sx SET sx.rescat=sx.rescat+(case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 where inserted.ref<>'' and sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and inserted.cativo=1  and inserted.fechada=0 and inserted.emconf=0 and inserted.emconf=0 and bo2.anulado=0 IF (select count(*) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.obistamp<>'' and inserted.qtt<>0 and bo2.anulado=0)>0 BEGIN UPDATE BI SET bi.fdata = bo.dataobra,bi.fno = bo.obrano,bi.adoc = convert(char(10),bo.obrano),bi.nmdoc = bo.nmdos,bi.ndoc = bo.ndos, bi.qtt2 = bi.qtt2+(select SUM(i.qtt) from inserted i where i.obistamp=inserted.obistamp group by i.obistamp) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join bo (nolock) on inserted.bostamp=bo.bostamp where inserted.obistamp<>'' and inserted.obistamp=bi.bistamp and inserted.emconf=0 and bo2.anulado=0 UPDATE BI SET bi.partes2 = bi.partes2+inserted.partes from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join bi (nolock) on bi.bistamp = inserted.obistampinner join deleted on inserted.bistamp=deleted.bistamp and deleted.emconf=1 and inserted.emconf=0 inner join ts (nolock) on ts.ndos = bi.ndos where inserted.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and bo2.anulado=0 END END if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado=0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)) BEGIN DECLARE @@MROW intSELECT @@MROW = @@ROWCOUNTIF @@MROW = 1BEGIN if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and bo2.anulado=0) BEGINUPDATE ST SET st.qttcli=st.qttcli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and st.ref=deleted.ref and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0UPDATE SA SET sa.rescli =sa.rescli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and bo2.anulado=0) BEGINUPDATE ST SET st.qttcat=st.qttcat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and st.ref=deleted.ref and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.rescat =sa.rescat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE ST SET st.qttfor=st.qttfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and st.ref=deleted.ref and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.resfor =sa.resfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resusr=1 and deleted.fechada=0 and bo2.anulado=0) UPDATE ST SET st.usrqtt=st.usrqtt-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and st.ref=deleted.ref and deleted.resusr=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE ST SET st.qttrec=st.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and st.ref=deleted.ref and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.qttrec =sa.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli=sx.rescli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat=sx.rescat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor =sx.resfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec=sx.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 END ELSE BEGIN if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.qttrec =sa.qttrec-case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.resrec=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))UPDATE st SET st.qttrec =st.qttrec-case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resrec=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescli = sa.rescli -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.rescli=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))UPDATE st SET st.qttcli = st.qttcli -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.rescli=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescat = sa.rescat -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.cativo=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))UPDATE st SET st.qttcat = st.qttcat -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.cativo=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.resfor = sa.resfor -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.resfor=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))UPDATE st SET st.qttfor = st.qttfor -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resfor=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))ENDif exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resusr=1 and deleted.fechada=0 and bo2.anulado=0)  UPDATE st SET st.usrqtt = st.usrqtt -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resusr=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resusr=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1))if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec =sx.qttrec-ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.resrec=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)  WHERE sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli = sx.rescli - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam and deleted.fechada=0 and deleted.rescli=1),0) FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0  inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)  where sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem  if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat = sx.rescat - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.cativo=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)  WHERE sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor = sx.resfor - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.resfor=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1)  WHERE sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem END if exists(select top 1 sl.slstamp from sl inner join deleted on deleted.ref<>'' and sl.bistamp=deleted.bistamp and deleted.bistamp<>'' and deleted.emconf=0 inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado=0) DELETE FROM SL from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) where deleted.ref<>'' and sl.bistamp=deleted.bistamp and deleted.bistamp<>'' and deleted.emconf=0 and bo2.anulado = 0if exists(select top 1 bi.bistamp from bi inner join deleted on deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0 inner join bo on deleted.bostamp=bo.bostamp inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado=0) BEGIN UPDATE BI SET bi.fno = 0,bi.adoc = ' ',bi.nmdoc = ' ',bi.ndoc = 0, bi.qtt2 = bi.qtt2-(SELECT SUM(deleted.qtt) FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) inner join bo (nolock) on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) inner join bo on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0 and bo2.anulado = 0UPDATE BI SET bi.partes2 = bi.partes2-(SELECT SUM(deleted.partes) FROM deleted inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) inner join bo (nolock) on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistamp and deleted.emconf=0) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join bi (nolock) on bi.bistamp = deleted.obistamp inner join inserted on deleted.bistamp=inserted.bistamp and (deleted.emconf=0 and inserted.emconf=1) inner join ts (nolock) on ts.ndos = bi.ndos where deleted.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and deleted.emconf=0 and bo2.anulado = 0END if exists(select top 1 biprecat.bistampfor from biprecat inner join deleted on biprecat.bistampfor=deleted.bistamp inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join ts on deleted.ndos=ts.ndos inner join ts2 on ts2.ts2stamp=ts.tsstamp where deleted.resfor=1 and ts2.precativo=1 and bo2.anulado=0) DELETE FROM BIPRECAT FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where biprecat.bistampfor=deleted.bistamp and bo2.anulado = 0if exists(select top 1 biprecat.bistampcli from biprecat inner join deleted on biprecat.bistampcli=deleted.bistamp inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join ts on deleted.ndos=ts.ndos where deleted.rescli=1 and ts.cativo=1 and bo2.anulado=0) DELETE FROM BIPRECAT FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where biprecat.bistampcli=deleted.bistamp and bo2.anulado = 0 END if exists (select deleted.marcada from deleted inner join inserted on deleted.bistamp=inserted.bistamp inner join bo2 on inserted.bostamp = bo2.bo2stamp where deleted.emconf=0 and inserted.emconf=0 and bo2.anulado=0) BEGIN DECLARE @@MROWUPD intSELECT @@MROWUPD = @@ROWCOUNTIF @@MROWUPD = 1BEGIN IF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and bo2.anulado=0) BEGINUPDATE ST SET st.qttcli=st.qttcli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and st.ref=deleted.ref and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0UPDATE SA SET sa.rescli =sa.rescli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and bo2.anulado=0) BEGINUPDATE ST SET st.qttcat=st.qttcat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and st.ref=deleted.ref and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.rescat =sa.rescat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE ST SET st.qttfor=st.qttfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and st.ref=deleted.ref and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.resfor =sa.resfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(resusr) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resusr=1 and deleted.fechada=0 and bo2.anulado=0) UPDATE ST SET st.usrqtt=st.usrqtt-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and st.ref=deleted.ref and deleted.resusr=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE ST SET st.qttrec=st.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and st.ref=deleted.ref and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 UPDATE SA SET sa.qttrec =sa.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and sa.ref=deleted.ref and sa.armazem=deleted.armazem and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli=sx.rescli-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.rescli=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat=sx.rescat-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.cativo=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor =sx.resfor-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.resfor=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec=sx.qttrec-(case when (deleted.qtt-deleted.qtt2)<0 then 0 else (deleted.qtt-deleted.qtt2) end) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and sx.ref=deleted.ref and sx.armazem=deleted.armazem and sx.cor=deleted.cor and sx.tam=deleted.tam and deleted.resrec=1 and deleted.fechada=0 and deleted.emconf=0 and bo2.anulado = 0 END ELSE BEGIN IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.qttrec =sa.qttrec-case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.resrec=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttrec =st.qttrec-case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resrec=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescli = sa.rescli -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.rescli=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttcli = st.qttcli -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.rescli=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescat = sa.rescat -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.cativo=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttcat = st.qttcat -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.cativo=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.resfor = sa.resfor -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazem and deleted.fechada=0 and deleted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=sa.ref and deleted.armazem=sa.armazemand deleted.fechada=0 and deleted.resfor=1) end WHERE sa.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttfor = st.qttfor -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resfor=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(resusr) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resusr=1 and deleted.fechada=0 and bo2.anulado=0)  UPDATE st SET st.usrqtt = st.usrqtt -case when (SELECT SUM(qtt-qtt2) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resusr=1) is null then 0 else (SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and deleted.ref=st.ref and deleted.fechada=0 and deleted.resusr=1) end WHERE st.ref IN (SELECT deleted.ref FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0) IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resrec=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec =sx.qttrec-ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.resrec=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  WHERE sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem  IF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.rescli=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli = sx.rescli - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tam and deleted.fechada=0 and deleted.rescli=1),0) FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0  inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  where sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem   IF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.cativo=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat = sx.rescat - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.cativo=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  WHERE sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem  IF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select deleted.marcada from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp where deleted.ref<>'' and deleted.resfor=1 and deleted.fechada=0 and deleted.cor<>'' and deleted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor = sx.resfor - ISNULL((SELECT SUM(case when qtt-qtt2<0 then 0 else qtt-qtt2 end) FROM deleted WHERE deleted.ref<>'' and (deleted.cor<>'' or deleted.tam<>'') and deleted.ref=sx.ref and deleted.armazem=sx.armazem and deleted.cor=sx.cor and deleted.tam=sx.tamand deleted.fechada=0 and deleted.resfor=1),0)  FROM deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0   inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  WHERE sx.ref=deleted.ref and sx.cor = deleted.cor and sx.tam = deleted.tam and sx.armazem = deleted.armazem END if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.fechada=0 and bo2.anulado=0 )  INSERT INTO SA (sastamp,armazem,ref) select distinct convert(char(5),inserted.armazem)+inserted.ref,inserted.armazem,inserted.ref from inserted where not EXISTS(select sa.armazem from sa where sa.ref=inserted.ref and sa.armazem=inserted.armazem) and ( inserted.rescli=1 or inserted.resfor=1 or inserted.resrec=1) and inserted.ref<>'' IF @@MROWUPD = 1BEGIN if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and bo2.anulado=0)  BEGINIF UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(ref) OR UPDATE(armazem) OR UPDATE(rescli) OR UPDATE(fechada) UPDATE SA SET sa.rescli=sa.rescli+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and sa.ref=inserted.ref and sa.armazem=inserted.armazem and inserted.rescli=1  and inserted.fechada=0 and bo2.anulado = 0 IF UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(ref) OR UPDATE(rescli) OR UPDATE(fechada) UPDATE ST SET st.qttcli=st.qttcli+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and st.ref=inserted.ref and inserted.rescli=1  and inserted.fechada=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescat=sa.rescat+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and sa.ref=inserted.ref and sa.armazem=inserted.armazem and inserted.cativo=1  and inserted.fechada=0 and bo2.anulado = 0 UPDATE ST SET st.qttcat=st.qttcat+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and st.ref=inserted.ref and inserted.cativo=1  and inserted.fechada=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor =1 and inserted.fechada=0 and bo2.anulado=0 )  BEGINUPDATE SA SET sa.resfor =sa.resfor+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and sa.ref=inserted.ref and sa.armazem=inserted.armazem and inserted.resfor=1  and inserted.fechada=0 and bo2.anulado = 0 UPDATE ST SET st.qttfor =st.qttfor+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and st.ref=inserted.ref and inserted.resfor=1  and inserted.fechada=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec =1 and inserted.fechada=0 and bo2.anulado=0 )  BEGINUPDATE SA SET sa.qttrec=sa.qttrec+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and sa.ref=inserted.ref and sa.armazem=inserted.armazem and inserted.resrec=1  and inserted.fechada=0 and bo2.anulado = 0 UPDATE ST SET st.qttrec =st.qttrec+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and st.ref=inserted.ref and inserted.resrec=1  and inserted.fechada=0 and bo2.anulado = 0 ENDIF UPDATE(ref) OR UPDATE(resusr) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resusr =1 and inserted.fechada=0 and bo2.anulado=0 )  UPDATE ST SET st.usrqtt =st.usrqtt+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and st.ref=inserted.ref and inserted.resusr=1  and inserted.fechada=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli=1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli =sx.rescli+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and inserted.rescli=1  and inserted.fechada=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat=sx.rescat+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and inserted.cativo=1  and inserted.fechada=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor =1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor =sx.resfor+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and inserted.resfor=1  and inserted.fechada=0 and bo2.anulado = 0 IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec =1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec =sx.qttrec+case when inserted.qtt-inserted.qtt2>0 then inserted.qtt-inserted.qtt2 else 0 end from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and sx.ref=inserted.ref and sx.armazem=inserted.armazem and sx.cor=inserted.cor and sx.tam=inserted.tam and inserted.resrec=1  and inserted.fechada=0 and bo2.anulado = 0 END ELSE BEGIN IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec =1 and inserted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.qttrec =sa.qttrec+case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazem and inserted.fechada=0 and inserted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazemand inserted.fechada=0 and inserted.resrec=1) end WHERE sa.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttrec =st.qttrec +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.resrec=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.resrec=1) end WHERE st.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli =1 and inserted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescli = sa.rescli +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazem and inserted.fechada=0 and inserted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazemand inserted.fechada=0 and inserted.rescli=1) end WHERE sa.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttcli = st.qttcli +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.rescli=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.rescli=1) end WHERE st.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo=1 and inserted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.rescat = sa.rescat +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazem and inserted.fechada=0 and inserted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazemand inserted.fechada=0 and inserted.cativo=1) end WHERE sa.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttcat = st.qttcat +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.cativo=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.cativo=1) end WHERE st.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor =1 and inserted.fechada=0 and bo2.anulado=0)  BEGINUPDATE SA SET sa.resfor = sa.resfor +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazem and inserted.fechada=0 and inserted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=sa.ref and inserted.armazem=sa.armazemand inserted.fechada=0 and inserted.resfor=1) end WHERE sa.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)UPDATE st SET st.qttfor = st.qttfor +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.resfor=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.resfor=1) end WHERE st.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)ENDIF UPDATE(ref) OR UPDATE(resusr) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resusr =1 and inserted.fechada=0 and bo2.anulado=0)  UPDATE st SET st.usrqtt = st.usrqtt +case when (SELECT SUM(qtt-qtt2) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.resusr=1) is null then 0 else (SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and inserted.ref=st.ref and inserted.fechada=0 and inserted.resusr=1) end WHERE st.ref IN (SELECT inserted.ref FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0)IF UPDATE(ref) OR UPDATE(resrec) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resrec =1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.qttrec =sx.qttrec+ ISNULL((SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tamand inserted.fechada=0 and inserted.resrec=1),0) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0  inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  WHERE sx.ref=inserted.ref and sx.cor = inserted.cor and sx.tam = inserted.tam and sx.armazem = inserted.armazem IF UPDATE(ref) OR UPDATE(rescli) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.rescli =1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescli = sx.rescli + ISNULL((SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tam and inserted.fechada=0 and inserted.rescli=1),0) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0  inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  WHERE sx.ref=inserted.ref and sx.cor = inserted.cor and sx.tam = inserted.tam and sx.armazem = inserted.armazem IF UPDATE(ref) OR UPDATE(cativo) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.cativo =1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.rescat = sx.rescat + ISNULL((SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tamand inserted.fechada=0 and inserted.cativo=1),0) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0  inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 WHERE sx.ref=inserted.ref and sx.cor = inserted.cor and sx.tam = inserted.tam and sx.armazem = inserted.armazem IF UPDATE(ref) OR UPDATE(resfor) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(fechada) OR UPDATE(qtt) OR UPDATE(qtt2) OR UPDATE(emconf) OR UPDATE(armazem) if exists (select inserted.marcada from INSERTED inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref<>'' and inserted.resfor =1 and inserted.fechada=0 and inserted.cor<>'' and inserted.tam<>'' and bo2.anulado=0)  UPDATE Sx SET sx.resfor = sx.resfor + ISNULL((SELECT SUM(case when qtt-qtt2>0 then qtt-qtt2 else 0 end) FROM inserted WHERE inserted.ref<>'' and (inserted.cor<>'' or inserted.tam<>'') and inserted.ref=sx.ref and inserted.armazem=sx.armazem and inserted.cor=sx.cor and inserted.tam=sx.tam and inserted.fechada=0 and inserted.resfor=1),0) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0  inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0  WHERE sx.ref=inserted.ref and sx.cor = inserted.cor and sx.tam = inserted.tam and sx.armazem = inserted.armazem END if exists (select inserted.marcada FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join sl on sl.bistamp=inserted.bistamp where inserted.ref='' and inserted.bistamp<>'' and bo2.anulado = 0) DELETE FROM SL FROM inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where inserted.ref='' and sl.bistamp=inserted.bistamp and inserted.bistamp<>'' and bo2.anulado = 0IF update(usrdata) or update(usrhora) or update(usrinis) or update(rdata) or update(ref) or update(design) or update(qtt) or update(lote) or update(armazem) or update(num1) or update(sltt) or update(esltt) or update(slvu) or update(eslvu) or update(slvumoeda) or update(iva) or update(ivaincl) or update(codigo) or update(usr1) or update(usr2) or update(usr3) or update(usr4) or update(usr5) or update(slttmoeda) or update(ttdeb) or update(ettdeb) or update(epcusto) or update(slvumoeda) or update(cor) or update(tam) or update(composto) or update(iva) or update(ivaincl) or update(stns) or update(ccusto) or update(ncusto) or update(obrano) or update(no) or update(nome) or update(bifref) if exists (select inserted.marcada from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on ts.ndos=inserted.ndos where ts.stocks=1 and inserted.ref<>'' and bo2.anulado=0) UPDATE SL set sl.usrdata=inserted.usrdata,sl.usrhora=inserted.usrhora,sl.usrinis=inserted.usrinis,sl.fref=case when ts.lifref=1 then inserted.bifref else bo.fref end,sl.ccusto=case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,sl.ncusto=case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,sl.nome=bo.nome,sl.codigo=inserted.codigo,sl.moeda=bo.moeda,sl.datalc=inserted.rdata,sl.design=inserted.design,sl.ref=inserted.ref,sl.lote=inserted.lote,sl.armazem=inserted.armazem,sl.qtt=inserted.qtt,sl.num1=inserted.num1,sl.tt=inserted.sltt,sl.vu=inserted.slvu,sl.ett=inserted.esltt,sl.evu=inserted.eslvu,sl.frcl=bo.no,sl.adoc=convert(char (10), bo.obrano),sl.cm=case when (ts.producao=1 and inserted.producao=1) or (ts2.trocaequi=1 and inserted.trocaequi=1) then ts.cm2stocks else ts.cmstocks end,sl.cmdesc=case when (ts.producao=1 and inserted.producao=1) or (ts2.trocaequi=1 and inserted.trocaequi=1) then ts.cm2desc else ts.cmdesc end,sl.composto=inserted.composto,sl.cor=inserted.cor,sl.tam=inserted.tam,sl.vumoeda=inserted.slvumoeda,sl.usr1=inserted.usr1,sl.usr2=inserted.usr2,sl.usr3=inserted.usr3,sl.usr4=inserted.usr4,sl.usr5=inserted.usr5,sl.ttmoeda=inserted.slttmoeda,sl.pcpond=inserted.pcusto,sl.cpoc=inserted.cpoc,sl.valiva=case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end, sl.trfa=ts.trfa,sl.evaliva=case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,sl.epcpond=inserted.epcusto,sl.stns=inserted.stns from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join ts on ts.ndos=inserted.ndos inner join ts2 on ts2.ts2stamp=ts.tsstamp  inner join bo2 on bo2.bo2stamp=inserted.bostamp where inserted.bistamp=sl.slstamp and ts.stocks=1 and inserted.bistamp IN (select deleted.bistamp from deleted) and bo2.anulado = 0 IF UPDATE(ndos) OR UPDATE(ref) OR UPDATE(usrdata) OR UPDATE(usrhora) OR UPDATE(usrinis) OR UPDATE(bifref) OR UPDATE(ccusto) OR UPDATE(ncusto) OR UPDATE(codigo) OR UPDATE(rdata) OR UPDATE(design) OR UPDATE(ref) OR UPDATE(lote) OR UPDATE(ar2mazem) OR UPDATE(qtt) OR UPDATE(num1) OR UPDATE(sltt) OR UPDATE(slvu) OR UPDATE(usr1) OR UPDATE(usr2) OR UPDATE(usr3) OR UPDATE(usr4) OR UPDATE(usr5) OR UPDATE(esltt) OR UPDATE(eslvu) OR UPDATE(composto) OR UPDATE(cor) OR UPDATE(tam) OR UPDATE(slvumoeda) OR UPDATE(slttmoeda) OR UPDATE(pcusto) OR UPDATE(cpoc) OR UPDATE(ivaincl) OR UPDATE(ttdeb) OR UPDATE(iva) OR UPDATE(ettdeb) OR UPDATE(epcusto) OR UPDATE(stns) OR UPDATE(emconf) if exists (select inserted.marcada from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on ts.ndos=inserted.ndos where ts.stocks=1 and ts.trfa=1 and inserted.ref<>'' and bo2.anulado=0) UPDATE SL set sl.usrdata=inserted.usrdata,sl.usrhora=inserted.usrhora,sl.usrinis=inserted.usrinis,sl.fref=case when ts.lifref=1 then inserted.bifref else bo.fref end,sl.ccusto=case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,sl.ncusto=case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,sl.nome=bo.nome,sl.codigo=inserted.codigo,sl.moeda=bo.moeda,sl.datalc=inserted.rdata,sl.design=inserted.design,sl.ref=inserted.ref,sl.lote=inserted.lote,sl.armazem=inserted.ar2mazem,sl.qtt=inserted.qtt,sl.num1=inserted.num1,sl.tt=inserted.sltt,sl.vu=inserted.slvu,sl.usr1=inserted.usr1,sl.usr2=inserted.usr2,sl.usr3=inserted.usr3,sl.usr4=inserted.usr4,sl.usr5=inserted.usr5,sl.ett=inserted.esltt,sl.evu=inserted.eslvu,sl.frcl=bo.no,sl.adoc=convert(char (10), bo.obrano),sl.cm=ts.cm2stocks,sl.cmdesc=ts.cm2desc,sl.composto=inserted.composto,sl.cor=inserted.cor,sl.tam=inserted.tam,sl.vumoeda=inserted.slvumoeda,sl.ttmoeda=inserted.slttmoeda,sl.pcpond=inserted.pcusto,sl.cpoc=inserted.cpoc,sl.valiva=case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end, sl.trfa=ts.trfa,sl.evaliva=case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,sl.epcpond=inserted.epcusto,sl.stns=inserted.stns from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on inserted.bostamp=bo.bostamp inner join ts on ts.ndos=inserted.ndos  inner join bo2 on bo2.bo2stamp=inserted.bostamp where sl.slstamp = substring('T'+inserted.bistamp,1,25) and ts.stocks=1 and ts.trfa=1 and inserted.bistamp IN (select deleted.bistamp from deleted) and bo2.anulado = 0 if exists (select inserted.marcada from inserted inner join bo on bo.bostamp=inserted.bostamp inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on inserted.ndos=ts.ndos where bo2.anulado=0 and not EXISTS(select slstamp from sl where sl.slstamp=inserted.bistamp) and ts.stocks=1 and inserted.ref<>'' ) INSERT INTO SL (slstamp,bistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,ref,armazem,qtt,lote,tt,vu,ett,evu,num1,frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,vumoeda,ttmoeda,pcpond,cpoc,valiva,trfa,evaliva,epcpond,stns )select inserted.bistamp,inserted.bistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BO',case when ts.lifref=1 then inserted.bifref else bo.fref end,case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,bo.nome,inserted.codigo,bo.moeda,inserted.rdata,inserted.design,inserted.ref,inserted.armazem,inserted.qtt,inserted.lote,inserted.sltt,inserted.slvu,inserted.esltt,inserted.eslvu,inserted.num1,bo.no,convert(char (10), bo.obrano),case when (ts.producao=1 and inserted.producao=1) or (ts2.trocaequi=1 or inserted.trocaequi=1) then ts.cm2stocks else ts.cmstocks end,case when (ts.producao=1 and inserted.producao=1) or (ts2.trocaequi=1 or inserted.trocaequi=1) then ts.cm2desc else ts.cmdesc end,inserted.composto,inserted.cor,inserted.tam,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.slvumoeda,inserted.slttmoeda,inserted.pcusto,inserted.cpoc,case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end,ts.trfa,case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,inserted.epcusto,inserted.stns from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on bo.bostamp=inserted.bostamp inner join ts on inserted.ndos=ts.ndos inner join ts2 on ts2.ts2stamp=ts.tsstamp  inner join bo2 on bo2.bo2stamp=inserted.bostamp where not EXISTS(select slstamp from sl where sl.slstamp=inserted.bistamp) and ts.stocks=1 and inserted.ref<>'' and bo2.anulado = 0 if exists (select inserted.marcada from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts on ts.ndos=inserted.ndos where ts.stocks=1 and ts.trfa=1 and inserted.ref<>'' and bo2.anulado=0) INSERT INTO SL (slstamp,bistamp,usrinis,usrhora,usrdata,ousrinis,ousrhora,ousrdata,origem,fref,ccusto,ncusto,nome,codigo,moeda,datalc,design,ref,armazem,qtt,lote,tt,vu,ett,evu,num1,frcl,adoc,cm,cmdesc,composto,cor,tam,usr1,usr2,usr3,usr4,usr5,vumoeda,ttmoeda,pcpond,cpoc,valiva,trfa,evaliva,epcpond,stns )select substring('T'+inserted.bistamp,1,25),inserted.bistamp,inserted.usrinis,inserted.usrhora,inserted.usrdata,inserted.ousrinis,inserted.ousrhora,inserted.ousrdata,'BO',case when ts.lifref=1 then inserted.bifref else bo.fref end,case when ts.tccusto=1 then inserted.ccusto else bo.ccusto end,case when ts.tncusto=1 then inserted.ncusto else bo.ncusto end,bo.nome,inserted.codigo,bo.moeda,inserted.rdata,inserted.design,inserted.ref,inserted.ar2mazem,inserted.qtt,inserted.lote,inserted.sltt,inserted.slvu,inserted.esltt,inserted.eslvu,inserted.num1,bo.no,convert(char (10), bo.obrano),ts.cm2stocks,ts.cm2desc,inserted.composto,inserted.cor,inserted.tam,inserted.usr1,inserted.usr2,inserted.usr3,inserted.usr4,inserted.usr5,inserted.slvumoeda,inserted.slttmoeda,inserted.pcusto,inserted.cpoc,case when inserted.ivaincl=1 then inserted.ttdeb-inserted.ttdeb/(1+inserted.iva/100) else inserted.ttdeb*inserted.iva/100 end,ts.trfa,case when inserted.ivaincl=1 then inserted.ettdeb-inserted.ettdeb/(1+inserted.iva/100) else inserted.ettdeb*inserted.iva/100 end,inserted.epcusto,inserted.stns from inserted inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo on bo.bostamp=inserted.bostamp inner join ts on inserted.ndos=ts.ndos  inner join bo2 on bo2.bo2stamp=inserted.bostamp where not EXISTS(select slstamp from sl where substring(sl.slstamp,1,25)=substring('T'+inserted.bistamp,1,25)) and ts.stocks=1 and ts.trfa=1  and inserted.ref<>space(18) and bo2.anulado = 0 IF update(qtt) and (exists (select deleted.marcada from deleted inner join inserted on deleted.bistamp=inserted.bistamp inner join bo2 on inserted.bostamp = bo2.bo2stamp where deleted.obistamp<>'' and bo2.anulado=0) or exists (select inserted.marcada from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where obistamp<>'' and bo2.anulado=0)) BEGIN UPDATE BI SET bi.fno = 0,bi.adoc = ' ',bi.nmdoc = ' ',bi.ndoc = 0, bi.qtt2 = bi.qtt2-(select SUM(d.qtt) from deleted d where d.bistamp=deleted.bistamp) from deleted inner join bo2 on deleted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join inserted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo (nolock) on deleted.bostamp=bo.bostamp where deleted.obistamp<>'' and deleted.obistamp=bi.bistampUPDATE BI SET bi.partes2 = bi.partes2-deleted.partes from deleted inner join bi (nolock) on bi.bistamp = deleted.obistamp inner join bo2 on deleted.bostamp = bo2.bo2stamp inner join ts (nolock) on ts.ndos = bi.ndos where deleted.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and bo2.anulado = 0 UPDATE BI SET bi.fdata = bo.dataobra,bi.fno = bo.obrano,bi.adoc = convert(char(10),bo.obrano),bi.nmdoc = bo.nmdos,bi.ndoc = bo.ndos, bi.qtt2 = bi.qtt2+(select SUM(i.qtt) from inserted i where i.obistamp=inserted.obistamp group by i.obistamp) from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp and bo2.anulado = 0 inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 inner join bo (nolock) on inserted.bostamp=bo.bostamp where inserted.obistamp<>'' and inserted.obistamp=bi.bistampUPDATE BI SET bi.partes2 = bi.partes2+inserted.partes from inserted inner join bi (nolock) on bi.bistamp = inserted.obistamp inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join ts (nolock) on ts.ndos = bi.ndos where inserted.obistamp<>'' and (ts.usam1 = 1 or ts.usam2 = 1 or ts.usam3 = 1) and bo2.anulado = 0 END IF UPDATE(fdata) and exists (select inserted.marcada from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp where bo2.anulado=0)UPDATE BO SET bo.ultfact=inserted.fdata from inserted inner join bo2 on inserted.bostamp = bo2.bo2stamp inner join deleted on deleted.bistamp=inserted.bistamp and deleted.emconf=0 and inserted.emconf=0 where inserted.bostamp=bo.bostamp and inserted.fdata>bo.ultfact and bo2.anulado = 0 END END GO

SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
GO
CREATE TRIGGER tr_ftrd_insert ON ftrd WITH ENCRYPTION FOR INSERT  AS SET NOCOUNT ONINSERT INTO CC (ccstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,cred,ecred,credm,credf,ecredf,credfm,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs) select 'C'+inserted.ftrdstamp,cc.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,cc.datalc,case when td.lancacli=0 then ft.no else ft2.c2no end,case when td.lancacli=0 then ft.nome else ft2.c2nome end,cc.cm,cc.cmdesc,'RD',cc.cobranca,inserted.vreg,inserted.evreg,inserted.vmreg,inserted.vreg,inserted.evreg,inserted.vmreg,cc.ivav1,cc.ivav2,cc.ivav3,cc.ivav4,cc.ivav5,cc.ivav6,cc.ivav7,cc.ivav8,cc.ivav9,cc.eivav1,cc.eivav2,cc.eivav3,cc.eivav4,cc.eivav5,cc.eivav6,cc.eivav7,cc.eivav8,cc.eivav9,cc.moeda,cc.nrdoc,cc.dataven,cc.fref,cc.intid,cc.cobrador,cc.rota,cc.ccusto,cc.ncusto,cc.zona,cc.vendedor,cc.vendnm,cc.segmento,cc.tipo,cc.pais,case when td.lancacli=0 then ft.estab else ft2.c2estab end,'Do cliente nº '+ltrim(str(cc.no)) from inserted,ft,ft2,td,cc where ft.anulado = 0 and cc.ccstamp=inserted.ccstamp and ft.ftstamp=inserted.ftstamp and ft2.ft2stamp=ft.ftstamp and td.ndoc=ft.ndoc and case when td.lancacli=0 then ft.no else ft2.c2no end<>cc.no INSERT INTO CC (ccstamp,rdstamp,usrdata,usrhora,usrinis,ousrdata,ousrhora,ousrinis,datalc,no,nome,cm,cmdesc,origem,cobranca,deb,edeb,debm,debf,edebf,debfm,ivav1,ivav2,ivav3,ivav4,ivav5,ivav6,ivav7,ivav8,ivav9,eivav1,eivav2,eivav3,eivav4,eivav5,eivav6,eivav7,eivav8,eivav9,moeda,nrdoc,dataven,fref,intid,cobrador,rota,ccusto,ncusto,zona,vendedor,vendnm,segmento,tipo,pais,estab,obs) select 'D'+inserted.ftrdstamp,cc.rdstamp,inserted.usrdata,inserted.usrhora,inserted.usrinis,inserted.ousrdata,inserted.ousrhora,inserted.ousrinis,cc.datalc,cc.no,cc.nome,cc.cm,cc.cmdesc,'RD',cc.cobranca,inserted.vreg,inserted.evreg,inserted.vmreg,inserted.vreg,inserted.evreg,inserted.vmreg,cc.ivav1,cc.ivav2,cc.ivav3,cc.ivav4,cc.ivav5,cc.ivav6,cc.ivav7,cc.ivav8,cc.ivav9,cc.eivav1,cc.eivav2,cc.eivav3,cc.eivav4,cc.eivav5,cc.eivav6,cc.eivav7,cc.eivav8,cc.eivav9,cc.moeda,cc.nrdoc,cc.dataven,cc.fref,cc.intid,cc.cobrador,cc.rota,cc.ccusto,cc.ncusto,cc.zona,cc.vendedor,cc.vendnm,cc.segmento,cc.tipo,cc.pais,cc.estab,'Regularizado no doc. '+rtrim(ft.nmdoc)+' nº '+ltrim(str(ft.fno)) from inserted,ft,ft2,td,cc where ft.anulado = 0 and cc.ccstamp=inserted.ccstamp and ft.ftstamp=inserted.ftstamp and ft2.ft2stamp=ft.ftstamp and td.ndoc=ft.ndoc and case when td.lancacli=0 then ft.no else ft2.c2no end<>cc.noUPDATE CC SET cc.difcambio = cc.difcambio + (select SUM(inserted.difcambio) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.edifcambio = cc.edifcambio + (select SUM(inserted.edifcambio) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.credf = cc.credf + (select SUM(inserted.vreg) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.ecredf = cc.ecredf + (select SUM(inserted.evreg) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp), cc.credfm = cc.credfm + (select SUM(inserted.vmreg) from inserted where inserted.ccstamp <> '' and inserted.ccstamp = cc.ccstamp) from cc inner join inserted on cc.ccstamp = inserted.ccstamp where inserted.ccstamp <> '' update cc set cc.debf = cc.debf + (select SUM(inserted.vreg) from inserted where inserted.multi = 0 and inserted.ftstamp <> '' and inserted.ftstamp = cc.ftstamp), cc.edebf = cc.edebf + (select SUM(inserted.evreg) from inserted where inserted.multi = 0 and inserted.ftstamp <> '' and inserted.ftstamp = cc.ftstamp), cc.debfm = cc.debfm + (select SUM(inserted.vmreg) from inserted where inserted.multi = 0 and inserted.ftstamp <> '' and inserted.ftstamp = cc.ftstamp) from cc inner join inserted on inserted.ftstamp = cc.ftstamp inner join ft on ft.ftstamp = cc.ftstamp inner join td on td.ndoc = ft.ndoc where ft.anulado = 0 and cc.ccstamp<>'R'+inserted.ftstamp and inserted.multi = 0 and td.ccautoreg = 0 update rd set rd.integrado=1 from inserted where rd.rdstamp=inserted.rdstamp GO
