BEGIN;

CREATE TABLE IF NOT EXISTS public.rastreabilidade_ordens (
    id_ordem bigint PRIMARY KEY,
    lote text,
    status text,
    data_criacao timestamp without time zone,
    data_inicio timestamp without time zone,
    produtos jsonb NOT NULL DEFAULT '[]'::jsonb,
    pesquisa_texto text NOT NULL DEFAULT ''
);

ALTER TABLE public.rastreabilidade_ordens
    ADD COLUMN IF NOT EXISTS lote text,
    ADD COLUMN IF NOT EXISTS status text,
    ADD COLUMN IF NOT EXISTS data_criacao timestamp without time zone,
    ADD COLUMN IF NOT EXISTS data_inicio timestamp without time zone,
    ADD COLUMN IF NOT EXISTS produtos jsonb NOT NULL DEFAULT '[]'::jsonb,
    ADD COLUMN IF NOT EXISTS pesquisa_texto text NOT NULL DEFAULT '';

CREATE INDEX IF NOT EXISTS idx_rastreabilidade_ordens_data_criacao
    ON public.rastreabilidade_ordens (data_criacao DESC, id_ordem DESC);

CREATE INDEX IF NOT EXISTS idx_rastreabilidade_ordens_status
    ON public.rastreabilidade_ordens (status);

ALTER TABLE public.rastreabilidade_ordens ENABLE ROW LEVEL SECURITY;

REVOKE ALL PRIVILEGES ON TABLE public.rastreabilidade_ordens
FROM anon, authenticated;

GRANT SELECT ON TABLE public.rastreabilidade_ordens TO anon;

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE public.rastreabilidade_ordens
TO service_role;

DROP POLICY IF EXISTS "public read only"
ON public.rastreabilidade_ordens;

CREATE POLICY "public read only"
ON public.rastreabilidade_ordens
FOR SELECT TO anon
USING (true);

COMMIT;
