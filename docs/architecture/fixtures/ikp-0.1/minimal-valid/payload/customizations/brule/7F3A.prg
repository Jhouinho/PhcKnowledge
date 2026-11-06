* Fixture IKP — definição fictícia, sem dados de cliente
IF EMPTY(FT.U_OBRA)
    MESSAGE("A obra é obrigatória.")
    RETURN .F.
ENDIF
RETURN .T.
