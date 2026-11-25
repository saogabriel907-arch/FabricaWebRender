BEGIN;

ALTER TABLE public.rastreabilidade_movimentacoes
    ADD COLUMN IF NOT EXISTS id_produto bigint;

CREATE INDEX IF NOT EXISTS idx_rastreabilidade_movimentacoes_produto
    ON public.rastreabilidade_movimentacoes (id_produto)
    WHERE id_produto IS NOT NULL;

NOTIFY pgrst, 'reload schema';

COMMIT;
