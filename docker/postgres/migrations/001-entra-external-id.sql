-- Rodar UMA vez em bancos que já existiam antes da integração com o Microsoft Entra External ID.
-- (O 01-users.sql já cria a coluna como NULL em bancos novos; isto é só para os antigos.)
--
-- ACA:  az containerapp exec -g "$RG_ACA" -n postgres-app --command "psql -U postgres -d eCommerceUsers"
-- AKS:  kubectl exec -it -n ecommerce-data postgres-0 -- psql -U postgres -d eCommerceUsers
-- e cole o comando abaixo.

ALTER TABLE public."Users" ALTER COLUMN "Password" DROP NOT NULL;
